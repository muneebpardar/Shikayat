using Microsoft.EntityFrameworkCore;
using Shikayat.Application.DTOs;
using Shikayat.Application.Interfaces;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;
using Shikayat.Infrastructure.Data;

namespace Shikayat.Infrastructure.Repositories
{
    public partial class ComplaintRepository : IComplaintRepository
    {
        private readonly AppDbContext _context;

        public ComplaintRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Complaint complaint)
        {
            await _context.Complaints.AddAsync(complaint);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Complaint>> GetComplaintsByCitizenIdAsync(string citizenId)
        {
            return await _context.Complaints
                .Include(c => c.SubCategory)       // Join SubCategory
                .ThenInclude(sc => sc.Parent)      // Join Parent (Department)
                .Where(c => c.CitizenId == citizenId)
                .OrderByDescending(c => c.CreatedAt) // Newest first
                .ToListAsync();
        }

        public async Task<Complaint> GetComplaintByIdAsync(int id)
        {
            var complaint = await _context.Complaints
                .Include(c => c.Citizen)
                .Include(c => c.SubCategory)
                .Include(c => c.Province)
                .Include(c => c.District)
                .Include(c => c.Tehsil)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (complaint != null)
            {
                complaint.Logs = await _context.ComplaintLogs
                    .Where(l => l.ComplaintId == id)
                    .Include(l => l.Sender)
                    .ToListAsync();
            }
            
            return complaint;
        }

        public async Task AddLogAsync(ComplaintLog log)
        {
            await _context.ComplaintLogs.AddAsync(log);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Complaint>> GetComplaintsByJurisdictionAsync(int? provinceId, int? districtId, int? tehsilId)
        {
            var query = _context.Complaints
                .Include(c => c.Citizen)
                .Include(c => c.SubCategory)
                    .ThenInclude(sc => sc.Parent) // Include Parent (Department) for filtering
                .Include(c => c.Province)
                .Include(c => c.District)
                .Include(c => c.Tehsil)
                .AsQueryable();

            // If NO location is passed (SuperAdmin), do nothing -> Return All.

            // If Province is passed
            if (provinceId.HasValue)
            {
                query = query.Where(c => c.ProvinceId == provinceId);
            }

            // If District is passed (It will override Province filter usually, or act as AND)
            if (districtId.HasValue)
            {
                query = query.Where(c => c.DistrictId == districtId);
            }

            // If Tehsil is passed
            if (tehsilId.HasValue)
            {
                query = query.Where(c => c.TehsilId == tehsilId);
            }

            return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        // Accept userId string with optional resolution details
        public async Task UpdateStatusAsync(int complaintId, ComplaintStatus status, string userId, string? resolutionNote = null, string? resolutionAttachmentPath = null)
        {
            var complaint = await _context.Complaints.FindAsync(complaintId);
            if (complaint != null)
            {
                var oldStatus = complaint.Status;
                complaint.Status = status;

                // If resolving, set resolved date and store resolution details
                if (status == ComplaintStatus.Resolved)
                {
                    complaint.ResolvedAt = DateTime.UtcNow;
                    complaint.ResolutionNote = resolutionNote;
                    complaint.ResolutionAttachmentPath = resolutionAttachmentPath;
                }
                else if (oldStatus == ComplaintStatus.Resolved && status != ComplaintStatus.Resolved)
                {
                    // If un-resolving, clear resolution details
                    complaint.ResolvedAt = null;
                    complaint.ResolutionNote = null;
                    complaint.ResolutionAttachmentPath = null;
                }

                // Create the System Log using the Admin's ID
                var logMessage = $"Status updated to {status}";
                if (status == ComplaintStatus.Resolved && !string.IsNullOrEmpty(resolutionNote))
                {
                    logMessage += $": {resolutionNote}";
                }

                _context.ComplaintLogs.Add(new ComplaintLog
                {
                    ComplaintId = complaintId,
                    Message = logMessage,
                    Timestamp = DateTime.UtcNow,
                    Type = LogType.StatusChange,
                    SenderId = userId
                });

                await _context.SaveChangesAsync();
            }
        }

        // New method for toggling importance
        public async Task ToggleImportanceAsync(int complaintId, bool isImportant)
        {
            var complaint = await _context.Complaints.FindAsync(complaintId);
            if (complaint != null)
            {
                complaint.IsImportant = isImportant;
                await _context.SaveChangesAsync();
            }
        }

        // ... inside the class ...

        public async Task<DashboardDto> GetDashboardStatsAsync(ApplicationUser user, IList<string> roles, int? drillProvinceId = null, int? drillDistrictId = null, int? drillTehsilId = null, int? departmentId = null)
        {
            var dto = new DashboardDto();
            var query = _context.Complaints
                .Include(c => c.SubCategory)
                    .ThenInclude(sc => sc.Parent)
                .Include(c => c.Province)
                .Include(c => c.District)
                .Include(c => c.Tehsil)
                .AsQueryable();

            // Filter by department if specified
            if (departmentId.HasValue)
            {
                query = query.Where(c => c.SubCategory.ParentId == departmentId);
            }

            // 1. SECURITY FILTERS (Who are you?)
            // Apply security filters based on role - these ensure users only see data they're authorized for
            // Only apply when NOT drilling down (navigation filters handle drill-down scenarios)
            if (roles.Contains("ProvincialAdmin") && !drillDistrictId.HasValue && !drillTehsilId.HasValue)
            {
                // Only apply security filter if NOT drilling down (when viewing province level)
                if (!drillProvinceId.HasValue)
                {
                    query = query.Where(c => c.ProvinceId == user.ProvinceId);
                }
            }
            if (roles.Contains("DistrictAdmin") && !drillDistrictId.HasValue && !drillTehsilId.HasValue)
            {
                query = query.Where(c => c.DistrictId == user.DistrictId);
            }
            if (roles.Contains("ZonalAdmin") && !drillTehsilId.HasValue)
            {
                query = query.Where(c => c.TehsilId == user.TehsilId);
            }

            // 2. NAVIGATION FILTERS (Where did you click?) - Apply drill-down filters
            // These restrict the query based on user navigation
            if (drillProvinceId.HasValue) 
            {
                query = query.Where(c => c.ProvinceId == drillProvinceId);
            }
            if (drillDistrictId.HasValue) 
            {
                query = query.Where(c => c.DistrictId == drillDistrictId);
                // For ProvincialAdmin: When drilling to district, ensure the district belongs to their province
                // This is needed because districts belong to provinces, and we need to verify the relationship
                if (roles.Contains("ProvincialAdmin") && user.ProvinceId.HasValue)
                {
                    // Always ensure the district is in the ProvincialAdmin's province
                    // If drillProvinceId is set, it should match user.ProvinceId (security check)
                    // If drillProvinceId is not set, use user.ProvinceId (authorization)
                    int provinceFilter = drillProvinceId ?? user.ProvinceId.Value;
                    query = query.Where(c => c.ProvinceId == provinceFilter);
                }
            }
            if (drillTehsilId.HasValue) 
            {
                query = query.Where(c => c.TehsilId == drillTehsilId);
            }

            // 3. AGGREGATE STATS (Top Cards)
            dto.TotalComplaints = await query.CountAsync();
            dto.ResolvedCount = await query.CountAsync(c => c.Status == ComplaintStatus.Resolved);
            dto.PendingCount = await query.CountAsync(c => c.Status != ComplaintStatus.Resolved);
            dto.ImportantCount = await query.CountAsync(c => c.IsImportant);

            // 4. DETERMINE VIEW (The Hierarchy Logic)

            // CASE A: Country Level View (SuperAdmin only, no drill down)
            if (roles.Contains("SuperAdmin") && !drillProvinceId.HasValue)
            {
                dto.PageTitle = "National Overview (Pakistan)";
                dto.UserRole = "SuperAdmin";
                dto.IsDrillDown = false;

                // Group by Province
                dto.SubRegions = await query
                    .GroupBy(c => c.Province)
                    .Select(g => new RegionStatDto
                    {
                        Id = g.Key.Id,
                        Name = g.Key.Name,
                        Total = g.Count(),
                        Resolved = g.Count(x => x.Status == ComplaintStatus.Resolved),
                        Pending = g.Count(x => x.Status != ComplaintStatus.Resolved),
                        Important = g.Count(x => x.IsImportant)
                    }).ToListAsync();
            }
        // CASE B: Province Level View (ProvincialAdmin OR SuperAdmin drilling into Province)
        else if ((roles.Contains("ProvincialAdmin") || drillProvinceId.HasValue) && !drillDistrictId.HasValue)
            {
                // Fetch Name for Title
                string pName = drillProvinceId.HasValue
                    ? (await _context.Locations.FindAsync(drillProvinceId))?.Name
                    : (await _context.Locations.FindAsync(user.ProvinceId))?.Name;

                dto.PageTitle = $"{pName} Province Dashboard";
                dto.UserRole = "ProvincialAdmin";
                dto.IsDrillDown = drillProvinceId.HasValue; // True if SuperAdmin clicked here

                // Group by District
                dto.SubRegions = await query
                    .GroupBy(c => c.District)
                    .Select(g => new RegionStatDto
                    {
                        Id = g.Key.Id,
                        Name = g.Key.Name,
                        Total = g.Count(),
                        Resolved = g.Count(x => x.Status == ComplaintStatus.Resolved),
                        Pending = g.Count(x => x.Status != ComplaintStatus.Resolved),
                        Important = g.Count(x => x.IsImportant)
                    }).ToListAsync();
            }
            // CASE C: District View (Shows List of Zones)
            else if ((roles.Contains("DistrictAdmin") || drillDistrictId.HasValue) && !drillTehsilId.HasValue)
            {
                // Fetch district name for title
                string districtName = drillDistrictId.HasValue
                    ? (await _context.Locations.FindAsync(drillDistrictId))?.Name ?? "District Overview"
                    : "District Overview";
                
                dto.PageTitle = districtName;
                dto.UserRole = roles.Contains("DistrictAdmin") ? "DistrictAdmin" : "ProvincialAdmin"; // Preserve original role for ProvincialAdmin drill-down
                dto.IsDrillDown = drillDistrictId.HasValue;
                
                // Group by Tehsil
                dto.SubRegions = await query
                    .GroupBy(c => c.Tehsil)
                    .Select(g => new RegionStatDto { Id = g.Key.Id, Name = g.Key.Name, Total = g.Count(), Resolved = g.Count(x => x.Status == ComplaintStatus.Resolved), Pending = g.Count(x => x.Status != ComplaintStatus.Resolved) })
                    .ToListAsync();
            }
            // CASE D: Zonal View (Shows Actual Complaints)
            else
            {
                // This hits when drilling down to a specific Tehsil (Zone)!
                dto.PageTitle = "Complaints List";
                dto.UserRole = "ZonalAdmin"; // Using Zonal layout for the list view
                dto.IsDrillDown = true;
                dto.Complaints = await query
                    .Include(c => c.Citizen)
                    .OrderByDescending(c => c.IsImportant).ThenByDescending(c => c.CreatedAt)
                    .ToListAsync();
            }

            return dto;
        }
    }
}
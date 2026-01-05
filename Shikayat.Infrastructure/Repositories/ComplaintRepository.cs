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

        public async Task<DashboardDto> GetDashboardStatsAsync(DashboardFilterDto filter)
        {
            var dto = new DashboardDto();
            var query = _context.Complaints
                .Include(c => c.SubCategory)
                    .ThenInclude(sc => sc.Parent)
                .Include(c => c.Province)
                .Include(c => c.District)
                .Include(c => c.Tehsil)
                .AsQueryable();

            // 1. APPLY FILTERS (From Service)
            if (filter.DepartmentId.HasValue) 
                query = query.Where(c => c.SubCategory.ParentId == filter.DepartmentId);

            if (filter.ProvinceId.HasValue)
                query = query.Where(c => c.ProvinceId == filter.ProvinceId);
            
            if (filter.DistrictId.HasValue)
                query = query.Where(c => c.DistrictId == filter.DistrictId);
            
            if (filter.TehsilId.HasValue)
                query = query.Where(c => c.TehsilId == filter.TehsilId);

            // Drill-down logic (Cumulative filters)
            if (filter.DrillProvinceId.HasValue)
                query = query.Where(c => c.ProvinceId == filter.DrillProvinceId);
            
            if (filter.DrillDistrictId.HasValue)
                query = query.Where(c => c.DistrictId == filter.DrillDistrictId);

            if (filter.DrillTehsilId.HasValue)
                query = query.Where(c => c.TehsilId == filter.DrillTehsilId);


            // 2. AGGREGATE STATS
            dto.TotalComplaints = await query.CountAsync();
            dto.ResolvedCount = await query.CountAsync(c => c.Status == ComplaintStatus.Resolved);
            dto.PendingCount = await query.CountAsync(c => c.Status != ComplaintStatus.Resolved);
            dto.ImportantCount = await query.CountAsync(c => c.IsImportant);

            // 3. DETERMINE VIEW HIERARCHY (Based on what filter was applied)
            // Logic: 
            // - If DrillTehsilId is set -> Show Complaints (Zonal View)
            // - If DrillDistrictId is set -> Show Tehsils
            // - If DrillProvinceId is set -> Show Districts
            // - If nothing set -> Show Provinces (National View)
            
            // NOTE: The "UserRole" property in DTO is redundant/presentation-concern if used for logic, 
            // but we'll populate it based on the view level for now or leave empty if Service sets it.
            // Actually, Service "knows" the role, so Service should set the Title and Role.
            // Metadata like "IsDrillDown" is also logic.
            
            // We just return data. The Service will enrich the DTO with titles.
            // BUT, the GroupBy logic depends on the level.
            
            // 3. DETERMINE VIEW HIERARCHY (Based on effective scope)
            // Logic: 
            // - If focused on Tehsil (Drill or Fixed) -> Show Complaints
            // - If focused on District (Drill or Fixed) -> Show Tehsils
            // - If focused on Province (Drill or Fixed) -> Show Districts
            // - Global -> Show Provinces
            
            if (filter.DrillTehsilId.HasValue || (filter.TehsilId.HasValue && !filter.DrillTehsilId.HasValue))
            {
                 // Handle overlap: If TehsilId is fixed, we are effectively drill-downed to it.
                 // BUT, we must ensure we don't double apply if both are same (Filter logic above checks values).
                 
                // Show Actual Complaints
                dto.Complaints = await query
                    .Include(c => c.Citizen)
                    .OrderByDescending(c => c.IsImportant).ThenByDescending(c => c.CreatedAt)
                    .ToListAsync();
                dto.IsDrillDown = true;
            }
            else if (filter.DrillDistrictId.HasValue || (filter.DistrictId.HasValue && !filter.DrillDistrictId.HasValue))
            {
                // Show Tehsils
                dto.IsDrillDown = true;
                dto.SubRegions = await query
                    .GroupBy(c => c.Tehsil)
                    .Select(g => new RegionStatDto { Id = g.Key.Id, Name = g.Key.Name, Total = g.Count(), Resolved = g.Count(x => x.Status == ComplaintStatus.Resolved), Pending = g.Count(x => x.Status != ComplaintStatus.Resolved), Important = g.Count(x => x.IsImportant) })
                    .ToListAsync();
            }
            else if (filter.DrillProvinceId.HasValue || (filter.ProvinceId.HasValue && !filter.DrillProvinceId.HasValue))
            {
                // Show Districts
                dto.IsDrillDown = true;
                dto.SubRegions = await query
                    .GroupBy(c => c.District)
                    .Select(g => new RegionStatDto { Id = g.Key.Id, Name = g.Key.Name, Total = g.Count(), Resolved = g.Count(x => x.Status == ComplaintStatus.Resolved), Pending = g.Count(x => x.Status != ComplaintStatus.Resolved), Important = g.Count(x => x.IsImportant) })
                    .ToListAsync();
            }
            else
            {
                // Default: Show Provinces
                dto.IsDrillDown = false;
                dto.SubRegions = await query
                    .GroupBy(c => c.Province)
                    .Select(g => new RegionStatDto { Id = g.Key.Id, Name = g.Key.Name, Total = g.Count(), Resolved = g.Count(x => x.Status == ComplaintStatus.Resolved), Pending = g.Count(x => x.Status != ComplaintStatus.Resolved), Important = g.Count(x => x.IsImportant) })
                    .ToListAsync();
            }

            return dto;
        }
    }
}
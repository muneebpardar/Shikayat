using Microsoft.EntityFrameworkCore;
using Shikayat.Application.DTOs;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shikayat.Infrastructure.Repositories
{
    public partial class ComplaintRepository
    {
        public async Task<AnalyticsDto> GetAnalyticsAsync(ApplicationUser user, IList<string> roles)
        {
            var dto = new AnalyticsDto
            {
                UserRole = roles.FirstOrDefault() ?? "Unknown"
            };

            // Base query with security filters
            var query = _context.Complaints
                .Include(c => c.SubCategory)
                    .ThenInclude(sc => sc.Parent)
                .Include(c => c.Province)
                .Include(c => c.District)
                .Include(c => c.Tehsil)
                .AsQueryable();

            // Apply security filters based on role
            if (roles.Contains("ProvincialAdmin") && user.ProvinceId.HasValue)
            {
                query = query.Where(c => c.ProvinceId == user.ProvinceId.Value);
                var province = await _context.Locations.FindAsync(user.ProvinceId.Value);
                dto.PageTitle = $"Analytics - {province?.Name ?? "Province"}";
            }
            else if (roles.Contains("DistrictAdmin") && user.DistrictId.HasValue)
            {
                query = query.Where(c => c.DistrictId == user.DistrictId.Value);
                dto.PageTitle = "Analytics - District";
            }
            else if (roles.Contains("ZonalAdmin") && user.TehsilId.HasValue)
            {
                query = query.Where(c => c.TehsilId == user.TehsilId.Value);
                dto.PageTitle = "Analytics - Zone";
            }
            else
            {
                dto.PageTitle = "Analytics - National Overview";
            }

            // Total Metrics
            dto.TotalComplaints = await query.CountAsync();
            dto.TotalResolved = await query.CountAsync(c => c.Status == ComplaintStatus.Resolved);
            dto.TotalPending = await query.CountAsync(c => c.Status != ComplaintStatus.Resolved);
            dto.TotalImportant = await query.CountAsync(c => c.IsImportant);
            dto.ResolutionRate = dto.TotalComplaints > 0 
                ? Math.Round((double)dto.TotalResolved / dto.TotalComplaints * 100, 2) 
                : 0;

            // Average Resolution Time
            var resolvedComplaints = await query
                .Where(c => c.Status == ComplaintStatus.Resolved && c.ResolvedAt.HasValue)
                .Select(c => new { c.CreatedAt, c.ResolvedAt })
                .ToListAsync();

            if (resolvedComplaints.Any())
            {
                var totalDays = resolvedComplaints
                    .Sum(c => (c.ResolvedAt.Value - c.CreatedAt).TotalDays);
                dto.AverageResolutionDays = Math.Round(totalDays / resolvedComplaints.Count, 2);
            }

            // Status Distribution
            dto.StatusDistribution = await query
                .GroupBy(c => c.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count);

            // Priority Distribution
            dto.PriorityDistribution = await query
                .GroupBy(c => c.Priority)
                .Select(g => new { Priority = g.Key.ToString(), Count = g.Count() })
                .ToDictionaryAsync(x => x.Priority, x => x.Count);

            // Top Regions by Complaints
            if (roles.Contains("SuperAdmin"))
            {
                // Top Provinces
                dto.TopProvincesByComplaints = await query
                    .GroupBy(c => new { c.Province.Id, c.Province.Name })
                    .Select(g => new RegionComplaintStatDto
                    {
                        Id = g.Key.Id,
                        Name = g.Key.Name,
                        TotalComplaints = g.Count(),
                        ResolvedComplaints = g.Count(c => c.Status == ComplaintStatus.Resolved),
                        PendingComplaints = g.Count(c => c.Status != ComplaintStatus.Resolved),
                        ImportantComplaints = g.Count(c => c.IsImportant)
                    })
                    .OrderByDescending(x => x.TotalComplaints)
                    .Take(10)
                    .ToListAsync();

                // Top Districts (National)
                dto.TopDistrictsByComplaints = await query
                    .GroupBy(c => new { c.District.Id, DistrictName = c.District.Name, ProvinceName = c.Province.Name })
                    .Select(g => new RegionComplaintStatDto
                    {
                        Id = g.Key.Id,
                        Name = $"{g.Key.DistrictName} ({g.Key.ProvinceName})",
                        TotalComplaints = g.Count(),
                        ResolvedComplaints = g.Count(c => c.Status == ComplaintStatus.Resolved),
                        PendingComplaints = g.Count(c => c.Status != ComplaintStatus.Resolved),
                        ImportantComplaints = g.Count(c => c.IsImportant)
                    })
                    .OrderByDescending(x => x.TotalComplaints)
                    .Take(15)
                    .ToListAsync();
            }
            else if (roles.Contains("ProvincialAdmin"))
            {
                // Top Districts (Province Level)
                dto.TopDistrictsByComplaints = await query
                    .GroupBy(c => new { c.District.Id, c.District.Name })
                    .Select(g => new RegionComplaintStatDto
                    {
                        Id = g.Key.Id,
                        Name = g.Key.Name,
                        TotalComplaints = g.Count(),
                        ResolvedComplaints = g.Count(c => c.Status == ComplaintStatus.Resolved),
                        PendingComplaints = g.Count(c => c.Status != ComplaintStatus.Resolved),
                        ImportantComplaints = g.Count(c => c.IsImportant)
                    })
                    .OrderByDescending(x => x.TotalComplaints)
                    .Take(15)
                    .ToListAsync();

                // Top Tehsils (Province Level)
                dto.TopTehsilsByComplaints = await query
                    .GroupBy(c => new { c.Tehsil.Id, TehsilName = c.Tehsil.Name, DistrictName = c.District.Name })
                    .Select(g => new RegionComplaintStatDto
                    {
                        Id = g.Key.Id,
                        Name = $"{g.Key.TehsilName} ({g.Key.DistrictName})",
                        TotalComplaints = g.Count(),
                        ResolvedComplaints = g.Count(c => c.Status == ComplaintStatus.Resolved),
                        PendingComplaints = g.Count(c => c.Status != ComplaintStatus.Resolved),
                        ImportantComplaints = g.Count(c => c.IsImportant)
                    })
                    .OrderByDescending(x => x.TotalComplaints)
                    .Take(20)
                    .ToListAsync();
            }

            // Calculate Resolution Rates for regions
            foreach (var region in dto.TopDistrictsByComplaints)
            {
                region.ResolutionRate = region.TotalComplaints > 0
                    ? Math.Round((double)region.ResolvedComplaints / region.TotalComplaints * 100, 2)
                    : 0;
            }

            foreach (var region in dto.TopTehsilsByComplaints)
            {
                region.ResolutionRate = region.TotalComplaints > 0
                    ? Math.Round((double)region.ResolvedComplaints / region.TotalComplaints * 100, 2)
                    : 0;
            }

            foreach (var region in dto.TopProvincesByComplaints)
            {
                region.ResolutionRate = region.TotalComplaints > 0
                    ? Math.Round((double)region.ResolvedComplaints / region.TotalComplaints * 100, 2)
                    : 0;
            }

            // Top Departments
            dto.TopDepartmentsByComplaints = await query
                .Where(c => c.SubCategory.ParentId != null)
                .GroupBy(c => new { c.SubCategory.Parent.Id, c.SubCategory.Parent.Name })
                .Select(g => new DepartmentStatDto
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    TotalComplaints = g.Count(),
                    ResolvedComplaints = g.Count(c => c.Status == ComplaintStatus.Resolved)
                })
                .OrderByDescending(x => x.TotalComplaints)
                .Take(10)
                .ToListAsync();

            var totalDeptComplaints = dto.TopDepartmentsByComplaints.Sum(d => d.TotalComplaints);
            foreach (var dept in dto.TopDepartmentsByComplaints)
            {
                dept.ResolutionRate = dept.TotalComplaints > 0
                    ? Math.Round((double)dept.ResolvedComplaints / dept.TotalComplaints * 100, 2)
                    : 0;
                dept.PercentageOfTotal = dto.TotalComplaints > 0
                    ? Math.Round((double)dept.TotalComplaints / dto.TotalComplaints * 100, 2)
                    : 0;
            }

            // Top Categories
            dto.TopCategoriesByComplaints = await query
                .GroupBy(c => new { c.SubCategory.Id, c.SubCategory.Name, DepartmentName = c.SubCategory.Parent != null ? c.SubCategory.Parent.Name : "Unknown" })
                .Select(g => new CategoryStatDto
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    DepartmentName = g.Key.DepartmentName,
                    TotalComplaints = g.Count(),
                    ResolvedComplaints = g.Count(c => c.Status == ComplaintStatus.Resolved)
                })
                .OrderByDescending(x => x.TotalComplaints)
                .Take(15)
                .ToListAsync();

            foreach (var cat in dto.TopCategoriesByComplaints)
            {
                cat.ResolutionRate = cat.TotalComplaints > 0
                    ? Math.Round((double)cat.ResolvedComplaints / cat.TotalComplaints * 100, 2)
                    : 0;
            }

            // Time-based Analytics (Last 12 Months)
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var monthlyData = await query
                .Where(c => c.CreatedAt >= twelveMonthsAgo)
                .GroupBy(c => new { Year = c.CreatedAt.Year, Month = c.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Total = g.Count(),
                    Resolved = g.Count(c => c.Status == ComplaintStatus.Resolved),
                    Pending = g.Count(c => c.Status != ComplaintStatus.Resolved)
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            dto.ComplaintsByMonth = monthlyData.Select(m => new TimeSeriesStatDto
            {
                Period = new DateTime(m.Year, m.Month, 1).ToString("MMM yyyy"),
                TotalComplaints = m.Total,
                ResolvedComplaints = m.Resolved,
                PendingComplaints = m.Pending,
                ResolutionRate = m.Total > 0 ? Math.Round((double)m.Resolved / m.Total * 100, 2) : 0
            }).ToList();

            // Peak Complaint Hours (24-hour analysis)
            var hourData = await query
                .GroupBy(c => c.CreatedAt.Hour)
                .Select(g => new { Hour = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            var totalByHour = await query.CountAsync();
            dto.PeakComplaintHours = hourData.Select(h => new PeakPeriodDto
            {
                Period = $"{h.Hour:D2}:00",
                ComplaintCount = h.Count,
                Percentage = totalByHour > 0 ? Math.Round((double)h.Count / totalByHour * 100, 2) : 0
            }).ToList();

            // Peak Complaint Days (Day of week)
            var dayNames = new[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            // Materialize complaints first, then group by DayOfWeek in memory
            var complaintsForDayAnalysis = await query
                .Select(c => new { c.CreatedAt })
                .ToListAsync();

            var totalByDay = complaintsForDayAnalysis.Count;
            var dayData = complaintsForDayAnalysis
                .GroupBy(c => (int)c.CreatedAt.DayOfWeek)
                .Select(g => new { DayOfWeek = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            dto.PeakComplaintDays = dayData.Select(d => new PeakPeriodDto
            {
                Period = dayNames[d.DayOfWeek],
                ComplaintCount = d.Count,
                Percentage = totalByDay > 0 ? Math.Round((double)d.Count / totalByDay * 100, 2) : 0
            }).ToList();

            return dto;
        }
    }
}


using Shikayat.Application.DTOs;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;

namespace Shikayat.Application.Interfaces
{
    public interface IComplaintRepository
    {
        Task AddAsync(Complaint complaint);
        Task<Complaint> GetComplaintByIdAsync(int id);
        Task<List<Complaint>> GetComplaintsByCitizenIdAsync(string citizenId);
        Task AddLogAsync(ComplaintLog log);
        // Update the signature to include 'drillTehsilId'
        Task<DashboardDto> GetDashboardStatsAsync(ApplicationUser user, IList<string> roles, int? drillProvinceId = null, int? drillDistrictId = null, int? drillTehsilId = null, int? departmentId = null);
        Task<List<Complaint>> GetComplaintsByJurisdictionAsync(int? provinceId, int? districtId, int? tehsilId);
        Task UpdateStatusAsync(int complaintId, ComplaintStatus status, string userId, string? resolutionNote = null, string? resolutionAttachmentPath = null);
        Task ToggleImportanceAsync(int complaintId, bool isImportant);
        Task<AnalyticsDto> GetAnalyticsAsync(ApplicationUser user, IList<string> roles);
    }
}

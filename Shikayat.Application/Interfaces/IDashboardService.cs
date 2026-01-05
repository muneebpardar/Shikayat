using Shikayat.Application.DTOs;
using Shikayat.Domain.Entities;

namespace Shikayat.Application.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardDto> GetDashboardStatsAsync(ApplicationUser user, IList<string> roles, int? drillProvinceId = null, int? drillDistrictId = null, int? drillTehsilId = null, int? departmentId = null);
    }
}

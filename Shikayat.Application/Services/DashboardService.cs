using Shikayat.Application.DTOs;
using Shikayat.Application.Interfaces;
using Shikayat.Application.Constants;
using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;

namespace Shikayat.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IComplaintRepository _complaintRepo;
        private readonly ILookupRepository _lookupRepo; // Needed for location names if we move logic here

        public DashboardService(IComplaintRepository complaintRepo, ILookupRepository lookupRepo)
        {
            _complaintRepo = complaintRepo;
            _lookupRepo = lookupRepo;
        }

        public async Task<DashboardDto> GetDashboardStatsAsync(ApplicationUser user, IList<string> roles, int? drillProvinceId = null, int? drillDistrictId = null, int? drillTehsilId = null, int? departmentId = null)
        {
            // 1. Construct Filter based on User Roles and Drill-down parameters
            // This logic was previously in the Repository. Now it is in the Service layer where business rules belong.
            
            var filter = new DashboardFilterDto
            {
                 ProvinceId = null,
                 DistrictId = null,
                 TehsilId = null,
                 DrillProvinceId = drillProvinceId,
                 DrillDistrictId = drillDistrictId,
                 DrillTehsilId = drillTehsilId,
                 DepartmentId = departmentId
            };

            // Security Filters
            if (roles.Contains(Roles.ProvincialAdmin))
            {
                 if (!drillDistrictId.HasValue && !drillTehsilId.HasValue && !drillProvinceId.HasValue)
                 {
                     filter.ProvinceId = user.ProvinceId; // Implicit Filter
                 }
                 // IMPORTANT: Even when drilling, Provincial Admin is restricted to their province.
                 // This logic must be enforced. Repository should trust the Filter DTO.
            }
            if (roles.Contains(Roles.DistrictAdmin))
            {
                 if (!drillDistrictId.HasValue && !drillTehsilId.HasValue)
                     filter.DistrictId = user.DistrictId;
            }
            if (roles.Contains(Roles.ZonalAdmin))
            {
                 if (!drillTehsilId.HasValue)
                     filter.TehsilId = user.TehsilId;
            }

            // NOTE: The Repository will need to be refactored to accept this Filter DTO and return data.
            // Currently, the implementations are coupled. I will implement the Service assuming the Repo will have a new method
            // OR I will call a refactored version of the existing one.
            // For this phase, I will delegate to the repository, but I will modify the Repository next to be simpler.
            // Wait, the Repository logic for "View Hierarchy" (Returning SubRegions vs Complaints List) 
            // is actually PRESENTATION Logic or APPLICATION Logic, not Infrastructure.
            // So we should get the Raw Data or Aggregates from Repo, and build the Response here.
            
            // However, to avoid massive rewriting of the EF Core query logic in one go, 
            // I will keep the hefty query in the repo for now, but call it via the Interface.
            // Ideally, we move the "Determining what to show" logic here.
            
            // Let's call the repository method (which I will refactor to use the Filter object).
            
            var dto = await _complaintRepo.GetDashboardStatsAsync(filter);
            
            // Post-Processing: Set Titles / Roles (Presentation/Logic)
            // Ideally this should be more robust, but carrying over previous logic:
            if (roles.Contains("SuperAdmin")) dto.UserRole = "SuperAdmin";
            else if (roles.Contains("ProvincialAdmin")) dto.UserRole = "ProvincialAdmin";
            else if (roles.Contains("DistrictAdmin")) dto.UserRole = "DistrictAdmin";
            else if (roles.Contains("ZonalAdmin")) dto.UserRole = "ZonalAdmin";

            // Title logic (can be expanded later by fetching location names via LookupRepo if needed)
            // Title logic
            if (string.IsNullOrEmpty(dto.PageTitle))
            {
                 if (roles.Contains("SuperAdmin") && !filter.DrillProvinceId.HasValue && !filter.DrillDistrictId.HasValue)
                     dto.PageTitle = "National Overview (Pakistan)";
                 else
                     dto.PageTitle = "Dashboard";
            }
            else if (!dto.PageTitle.Contains("Overview"))
            {
                 // Keep existing logic if any (currently unreachable as Repo returns null, but safe)
            }
            
            return dto;
        }
    }
}

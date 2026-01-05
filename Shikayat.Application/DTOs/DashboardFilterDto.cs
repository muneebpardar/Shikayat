using Shikayat.Domain.Entities;
using Shikayat.Domain.Enums;

namespace Shikayat.Application.DTOs
{
    public class DashboardFilterDto
    {
        // Security Filters
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? TehsilId { get; set; }

        // Drill-Down / Navigation Filters
        public int? DrillProvinceId { get; set; }
        public int? DrillDistrictId { get; set; }
        public int? DrillTehsilId { get; set; }
        
        // Content Filters
        public int? DepartmentId { get; set; }
    }
}

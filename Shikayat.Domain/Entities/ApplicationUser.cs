using Microsoft.AspNetCore.Identity;

namespace Shikayat.Domain.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }
        public string? CNIC { get; set; }

        // Jurisdiction Scope
        public int? ProvinceId { get; set; }
        public int? DistrictId { get; set; }
        public int? TehsilId { get; set; }

        public int? DepartmentId { get; set; }
    }
}
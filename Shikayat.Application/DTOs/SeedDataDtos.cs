namespace Shikayat.Application.DTOs
{
    // DTO for Complaints (complaints.json)
    public class ComplaintSeedDto
    {
        public string Subject { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Zone { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
    }

    // DTOs for Locations (locations.json)
    public class ProvinceDto
    {
        public string Name { get; set; }
        public List<DistrictDto> Districts { get; set; }
    }

    public class DistrictDto
    {
        public string Name { get; set; }
        public List<string> Tehsils { get; set; }
    }

    // DTOs for Categories (categories.json)
    public class CategoryDto
    {
        public string Name { get; set; }
        public List<string> SubCategories { get; set; }
    }
}
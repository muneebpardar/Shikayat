using Shikayat.Domain.Enums; // We will add Enums in a moment

namespace Shikayat.Domain.Entities
{
    public class Location
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public LocationType Type { get; set; }
        public int? ParentId { get; set; }
        public Location Parent { get; set; }
        // Navigation for children (e.g., A Province has many Districts)
        public ICollection<Location> Children { get; set; }
    }
}
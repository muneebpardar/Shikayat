using Shikayat.Domain.Entities;

namespace Shikayat.Application.DTOs
{
    public class DashboardDto
    {
        // 1. Context (Where are we?)
        public string PageTitle { get; set; } // e.g., "Pakistan Overview" or "Sindh Dashboard"
        public string UserRole { get; set; }  // To control UI elements
        public bool IsDrillDown { get; set; } // Are we viewing a sub-region?

        // 2. Top Cards (The "Big Numbers")
        public int TotalComplaints { get; set; }
        public int ResolvedCount { get; set; }
        public int PendingCount { get; set; }
        public int ImportantCount { get; set; }
        
        // Suggestions
        public int TotalSuggestions { get; set; }
        public int ResolvedSuggestions { get; set; }
        public int PendingSuggestions { get; set; }

        // 3. The List Data (For Table/Charts)
        // If we are at Country/Prov/District level, we show Regions.
        public List<RegionStatDto> SubRegions { get; set; } = new List<RegionStatDto>();

        // If we are at the lowest level (Zone), we show actual Tickets.
        public List<Complaint> Complaints { get; set; } = new List<Complaint>();
    }

    public class RegionStatDto
    {
        public int Id { get; set; }       // The ID to drill down into
        public string Name { get; set; }  // "Sindh" or "Karachi Central"
        public int Total { get; set; }
        public int Resolved { get; set; }
        public int Pending { get; set; }
        public int Important { get; set; }
    }
}
namespace Shikayat.Application.DTOs
{
    public class AnalyticsDto
    {
        public string PageTitle { get; set; }
        public string UserRole { get; set; }
        
        // Top Regions by Complaints
        public List<RegionComplaintStatDto> TopDistrictsByComplaints { get; set; } = new();
        public List<RegionComplaintStatDto> TopTehsilsByComplaints { get; set; } = new();
        public List<RegionComplaintStatDto> TopProvincesByComplaints { get; set; } = new();
        
        // Status Distribution
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
        
        // Department/Category Analytics
        public List<DepartmentStatDto> TopDepartmentsByComplaints { get; set; } = new();
        public List<CategoryStatDto> TopCategoriesByComplaints { get; set; } = new();
        
        // Time-based Analytics
        public List<TimeSeriesStatDto> ComplaintsByMonth { get; set; } = new();
        public List<TimeSeriesStatDto> ResolutionRateByMonth { get; set; } = new();
        
        // Performance Metrics
        public double AverageResolutionDays { get; set; }
        public int TotalComplaints { get; set; }
        public int TotalResolved { get; set; }
        public int TotalPending { get; set; }
        public double ResolutionRate { get; set; }
        public int TotalImportant { get; set; }
        
        // Priority Distribution
        public Dictionary<string, int> PriorityDistribution { get; set; } = new();
        
        // Peak Periods
        public List<PeakPeriodDto> PeakComplaintHours { get; set; } = new();
        public List<PeakPeriodDto> PeakComplaintDays { get; set; } = new();
    }
    
    public class RegionComplaintStatDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TotalComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public double ResolutionRate { get; set; }
        public int ImportantComplaints { get; set; }
    }
    
    public class DepartmentStatDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TotalComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public double ResolutionRate { get; set; }
        public double PercentageOfTotal { get; set; }
    }
    
    public class CategoryStatDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DepartmentName { get; set; }
        public int TotalComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public double ResolutionRate { get; set; }
    }
    
    public class TimeSeriesStatDto
    {
        public string Period { get; set; } // e.g., "2024-01", "Jan 2024"
        public int TotalComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public double ResolutionRate { get; set; }
    }
    
    public class PeakPeriodDto
    {
        public string Period { get; set; } // e.g., "09:00", "Monday"
        public int ComplaintCount { get; set; }
        public double Percentage { get; set; }
    }
}


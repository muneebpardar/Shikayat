using Shikayat.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Shikayat.Domain.Entities
{
    public class Suggestion
    {
        public int Id { get; set; }
        public string TicketId { get; set; }

        public string Subject { get; set; }
        public string Description { get; set; }

        // Relationships
        public string CitizenId { get; set; }
        public ApplicationUser Citizen { get; set; }

        public int SubCategoryId { get; set; }
        public Category SubCategory { get; set; }

        // Location Routing
        public int ProvinceId { get; set; }
        public int DistrictId { get; set; }
        public int TehsilId { get; set; }

        // Navigation properties for easy access to names
        public Location Province { get; set; }
        public Location District { get; set; }
        public Location Tehsil { get; set; }

        // Attachments
        public string? AttachmentPath { get; set; }

        public bool IsImportant { get; set; } = false;
        public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
        public ComplaintPriority Priority { get; set; } = ComplaintPriority.Normal;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        // Response/Notes
        public string? ResponseNote { get; set; }
        public string? ResponseAttachmentPath { get; set; }

        // Chat Thread
        public ICollection<ComplaintLog> Logs { get; set; }
    }
}


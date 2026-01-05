using Shikayat.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema; // Required for [ForeignKey]

namespace Shikayat.Domain.Entities
{
    public class ComplaintLog
    {
        public int Id { get; set; }
        public int? ComplaintId { get; set; }
        public int? SuggestionId { get; set; }

        public string SenderId { get; set; }

        [ForeignKey("SenderId")]
        public virtual ApplicationUser Sender { get; set; }

        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public LogType Type { get; set; }
    }
}
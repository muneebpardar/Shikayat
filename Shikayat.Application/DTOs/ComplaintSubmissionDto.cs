using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Required for IFormFile (Attachments)

namespace Shikayat.Application.DTOs
{
    public class ComplaintSubmissionDto
    {
        [Required]
        [Display(Name = "Complaint Subject")]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "Detailed Description")]
        public string Description { get; set; }

        // --- Categories (L1 is just for filtering L2) ---
        [Required]
        [Display(Name = "Department")]
        public int SelectedDepartmentId { get; set; } // L1

        [Required]
        [Display(Name = "Sub-Category")]
        public int SelectedSubCategoryId { get; set; } // L2

        // --- Location Hierarchy ---
        [Required]
        [Display(Name = "Province")]
        public int SelectedProvinceId { get; set; }

        [Required]
        [Display(Name = "District")]
        public int SelectedDistrictId { get; set; }

        [Required]
        [Display(Name = "Tehsil (Zone)")]
        public int SelectedTehsilId { get; set; }

        // Optional: File Upload (we will implement logic later)
        [Display(Name = "Attachment (Image/PDF)")]
        public IFormFile? Attachment { get; set; }
    }
}
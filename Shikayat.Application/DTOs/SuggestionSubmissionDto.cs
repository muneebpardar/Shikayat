using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Shikayat.Application.DTOs
{
    public class SuggestionSubmissionDto
    {
        [Required]
        [Display(Name = "Suggestion Subject")]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "Detailed Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Department")]
        public int SelectedDepartmentId { get; set; }

        [Required]
        [Display(Name = "Sub-Category")]
        public int SelectedSubCategoryId { get; set; }

        [Required]
        [Display(Name = "Province")]
        public int SelectedProvinceId { get; set; }

        [Required]
        [Display(Name = "District")]
        public int SelectedDistrictId { get; set; }

        [Required]
        [Display(Name = "Tehsil (Zone)")]
        public int SelectedTehsilId { get; set; }

        [Display(Name = "Attachment (Image/PDF)")]
        public IFormFile? Attachment { get; set; }
    }
}


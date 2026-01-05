using System.ComponentModel.DataAnnotations;

namespace Shikayat.Application.DTOs
{
    public class RegisterDto
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required]
        [Display(Name = "CNIC Number")]
        [StringLength(15, MinimumLength = 13, ErrorMessage = "CNIC must be 13-15 digits")]
        public string CNIC { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }
}
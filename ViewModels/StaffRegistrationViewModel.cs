using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.ViewModels
{
    public class StaffRegistrationViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public required string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public required string LastName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public required string ConfirmPassword { get; set; }

        [Required]
        public required string Address { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        public required string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Staff Role")]
        public required string StaffRole { get; set; }
    }
} 
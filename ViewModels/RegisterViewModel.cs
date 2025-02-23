using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public required string ConfirmPassword { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public required string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public required string LastName { get; set; }

        [Required]
        public required string Address { get; set; }

        [Required]
        [Display(Name = "Lot Number")]
        public required string LotNumber { get; set; }

        [Required]
        [Display(Name = "Block Number")]
        public required string BlockNumber { get; set; }
    }
}

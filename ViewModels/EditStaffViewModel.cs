using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.ViewModels
{
    public class EditStaffViewModel
    {
        [Required]
        public required string Id { get; set; }

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
        public required string Address { get; set; }

        [Required]
        [Display(Name = "Phone Number")]
        public required string PhoneNumber { get; set; }

        [Required]
        [Display(Name = "Staff Role")]
        public required string StaffRole { get; set; }
    }
} 
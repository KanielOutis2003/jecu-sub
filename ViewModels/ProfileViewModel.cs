using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.ViewModels
{
    public class ProfileViewModel
    {
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

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Current Profile Picture")]
        public string? ExistingProfilePicture { get; set; }

        [Display(Name = "Upload New Profile Picture")]
        public IFormFile? ProfilePicture { get; set; }
    }
} 
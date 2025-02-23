using SubdivisionWebsite.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SubdivisionWebsite.ViewModels
{
    public class UserProfileViewModel
    {
        [Required]
        [Display(Name = "First Name")]
        public required string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public required string LastName { get; set; }

        [Required]
        [EmailAddress]
        public virtual string? Email { get; set; }


        [Required]
        public required string Address { get; set; }

        [Required]
        [Display(Name = "Lot Number")]
        public required string LotNumber { get; set; }

        [Required]
        [Display(Name = "Block Number")]
        public required string BlockNumber { get; set; }

        [Display(Name = "Profile Picture")]
        public IFormFile? ProfilePicture { get; set; } // Nullable because it's optional

        public string? ExistingProfilePicture { get; set; } // Nullable since the user might not have one yet

        [Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

    }
}

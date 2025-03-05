using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.Models
{
    public enum UserType
    {
        Admin,
        Staff,
        Homeowner
    }

    public class ApplicationUser : IdentityUser
    {
        [Required]
        public required string FirstName { get; set; }

        [Required]
        public required string LastName { get; set; }

        [Required]
        public required string Address { get; set; }

        [Required]
        public required string LotNumber { get; set; }

        [Required]
        public required string BlockNumber { get; set; }

        public string? ProfilePicture { get; set; } = "default.png";

        public UserType UserType { get; set; }

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public string? StaffRole { get; set; }
    }
}

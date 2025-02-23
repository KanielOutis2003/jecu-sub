using Microsoft.AspNetCore.Identity;

namespace SubdivisionWebsite.Models
{
    public class ApplicationUser : IdentityUser
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Address { get; set; }
        public required string LotNumber { get; set; }
        public required string BlockNumber { get; set; }
        public UserType UserType { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public required string ProfilePicture { get; set; }
    }

    public enum UserType
    {
        Homeowner,
        Administrator,
        Staff
    }
}

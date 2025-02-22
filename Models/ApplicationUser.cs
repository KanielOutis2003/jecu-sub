using Microsoft.AspNetCore.Identity;

namespace SubdivisionWebsite.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public string LotNumber { get; set; }
        public string BlockNumber { get; set; }
        public UserType UserType { get; set; }
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public string ProfilePicture { get; set; }
    }

    public enum UserType
    {
        Homeowner,
        Administrator,
        Staff
    }
} 
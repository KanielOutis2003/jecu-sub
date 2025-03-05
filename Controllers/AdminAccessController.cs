using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SubdivisionWebsite.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace SubdivisionWebsite.Controllers
{
    public class AdminAccessController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AdminAccessController> _logger;

        public AdminAccessController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AdminAccessController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /AdminAccess/DirectLogin
        public async Task<IActionResult> DirectLogin()
        {
            // First, sign out any current user
            await _signInManager.SignOutAsync();
            
            // Find the admin user
            var adminEmail = "admin@subdivision.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                _logger.LogError("Admin user not found in database");
                ViewBag.ErrorMessage = "Admin user not found in database. Please check if the database is properly seeded.";
                return View("Error");
            }
            
            // Check if user is in admin role
            var isInAdminRole = await _userManager.IsInRoleAsync(adminUser, "Admin");
            if (!isInAdminRole)
            {
                _logger.LogError("User exists but is not in Admin role");
                ViewBag.ErrorMessage = "User exists but is not in Admin role. Please check role assignments.";
                return View("Error");
            }
            
            // Log in the admin user directly
            await _signInManager.SignInAsync(adminUser, isPersistent: false);
            _logger.LogInformation($"Admin user {adminEmail} logged in directly");
            
            return RedirectToAction("Dashboard", "Admin");
        }
        
        // GET: /AdminAccess/CheckAdminUser
        public async Task<IActionResult> CheckAdminUser()
        {
            var adminEmail = "admin@subdivision.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                return Content("Admin user not found in database");
            }
            
            var isInAdminRole = await _userManager.IsInRoleAsync(adminUser, "Admin");
            var passwordValid = await _userManager.CheckPasswordAsync(adminUser, "Admin@123");
            
            return Content($"Admin user exists: Yes\n" +
                          $"Email: {adminUser.Email}\n" +
                          $"UserName: {adminUser.UserName}\n" +
                          $"In Admin Role: {isInAdminRole}\n" +
                          $"Password Valid: {passwordValid}\n" +
                          $"Email Confirmed: {adminUser.EmailConfirmed}\n" +
                          $"User Type: {adminUser.UserType}");
        }
        
        // GET: /AdminAccess/CreateAdminUser
        public async Task<IActionResult> CreateAdminUser()
        {
            try
            {
                var adminEmail = "admin@subdivision.com";
                var adminPassword = "Admin@123";
                var existingUser = await _userManager.FindByEmailAsync(adminEmail);
                
                if (existingUser != null)
                {
                    // Delete existing user first
                    await _userManager.DeleteAsync(existingUser);
                    _logger.LogInformation("Deleted existing admin user");
                }
                
                // Create a new admin user with ALL required properties
                var adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Admin",
                    LastName = "User",
                    Address = "Main Office",        // Required property
                    LotNumber = "Admin",            // Required property
                    BlockNumber = "Admin",          // Required property
                    StaffRole = "Administrator",    // Required property
                    EmailConfirmed = true,
                    UserType = UserType.Admin,
                    ProfilePicture = "default.png"
                };
                
                var result = await _userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to create admin user: {errors}");
                    return Content($"Failed to create admin user: {errors}");
                }
                
                var roleResult = await _userManager.AddToRoleAsync(adminUser, "Admin");
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    _logger.LogError($"Failed to add admin role: {errors}");
                    return Content($"Failed to add admin role: {errors}");
                }
                
                return Content("Admin user created successfully. You can now try to log in or use the DirectLogin action.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during admin user creation");
                return Content($"Exception: {ex.Message}\n\nStack trace: {ex.StackTrace}");
            }
        }
    }
} 
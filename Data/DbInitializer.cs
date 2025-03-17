using Microsoft.AspNetCore.Identity;
using SubdivisionWebsite.Models;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace SubdivisionWebsite.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AppDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

            // Ensure database is created and migrated
            logger.LogInformation("Ensuring database exists and is up to date");
            context.Database.EnsureCreated();
            
            // Create roles if they don't exist
            string[] roles = { "Admin", "Staff", "Homeowner" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation($"Created role: {role}");
                }
            }

            // Create admin user if it doesn't exist
            var adminEmail = "admin@subdivision.com";
            var adminPassword = "Admin@123";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Administrator",
                    Address = "Main Office",
                    PhoneNumber = "1234567890",
                    EmailConfirmed = true,
                    LotNumber = "Admin",
                    BlockNumber = "Admin",
                    UserType = UserType.Admin,
                    ProfilePicture = "default-profile.png"
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    logger.LogInformation("Admin user created successfully");
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Admin role assigned successfully");
                    }
                    else
                    {
                        logger.LogError($"Failed to assign Admin role: {string.Join(", ", roleResult.Errors)}");
                    }
                }
                else
                {
                    logger.LogError($"Failed to create admin user: {string.Join(", ", result.Errors)}");
                    // Log the exception details
                    foreach (var error in result.Errors)
                    {
                        logger.LogError($"Error: {error.Description}");
                    }
                }
            }
            else
            {
                // Ensure existing admin is in Admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    var roleResult = await userManager.AddToRoleAsync(adminUser, "Admin");
                    if (roleResult.Succeeded)
                    {
                        logger.LogInformation("Admin role assigned to existing user");
                    }
                }

                // Reset password for existing admin (optional, remove if not needed)
                var token = await userManager.GeneratePasswordResetTokenAsync(adminUser);
                var resetResult = await userManager.ResetPasswordAsync(adminUser, token, adminPassword);
                if (resetResult.Succeeded)
                {
                    logger.LogInformation("Admin password reset successfully");
                }
            }

            // Verify admin account
            var verifyUser = await userManager.FindByEmailAsync(adminEmail);
            if (verifyUser != null)
            {
                var isInRole = await userManager.IsInRoleAsync(verifyUser, "Admin");
                logger.LogInformation($"Admin user exists: {verifyUser.Email}, IsInRole 'Admin': {isInRole}");
            }
        }
    }
} 
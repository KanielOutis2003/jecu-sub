using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Data;
using SubdivisionWebsite.Models;
using SubdivisionWebsite.Services;
using System.Threading.Tasks;
using System.IO;
using System;

namespace SubdivisionWebsite.Controllers
{
    [Authorize(Roles = "Homeowner")]
    public class HomeownerController : BaseController
    {
        private readonly IActivityLogService _activityLogService;

        public HomeownerController(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IWebHostEnvironment webHostEnvironment,
            IActivityLogService activityLogService)
            : base(context, userManager, webHostEnvironment)
        {
            _activityLogService = activityLogService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get recent activities for the dashboard
            ViewBag.RecentActivities = await _activityLogService.GetRecentActivitiesAsync(5);

            return View();
        }

        public async Task<IActionResult> CommunityForum()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Log the forum access
            await _activityLogService.LogActivityAsync(
                description: "Accessed community forum",
                module: "Forum",
                action: "Access",
                userId: currentUser.Id
            );

            return View();
        }

        public async Task<IActionResult> ServiceRequests()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Log the service requests access
            await _activityLogService.LogActivityAsync(
                description: "Accessed service requests",
                module: "Service",
                action: "Access",
                userId: currentUser.Id
            );

            return View();
        }

        public async Task<IActionResult> SecurityFeatures()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Log the security features access
            await _activityLogService.LogActivityAsync(
                description: "Accessed security features",
                module: "Security",
                action: "Access",
                userId: currentUser.Id
            );

            return View();
        }

        public new async Task<IActionResult> Profile()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Log the profile access
            await _activityLogService.LogActivityAsync(
                description: "Accessed profile page",
                module: "Profile",
                action: "Access",
                userId: currentUser.Id
            );

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(ApplicationUser model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                currentUser.FirstName = model.FirstName;
                currentUser.LastName = model.LastName;
                currentUser.PhoneNumber = model.PhoneNumber;
                currentUser.Address = model.Address;
                currentUser.LotNumber = model.LotNumber;
                currentUser.BlockNumber = model.BlockNumber;

                var result = await _userManager.UpdateAsync(currentUser);
                if (result.Succeeded)
                {
                    // Log the profile update
                    await _activityLogService.LogActivityAsync(
                        description: "Updated profile information",
                        module: "Profile",
                        action: "Update",
                        userId: currentUser.Id
                    );

                    TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                    return RedirectToAction(nameof(Profile));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View("Profile", model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfilePicture(IFormFile profilePicture)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (profilePicture != null && profilePicture.Length > 0 && _webHostEnvironment != null)
            {
                // Create profile pictures directory if it doesn't exist
                var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "profile");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(profilePicture.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save the file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                // Update user profile picture path
                currentUser.ProfilePicture = $"/images/profile/{fileName}";
                await _userManager.UpdateAsync(currentUser);

                // Log the profile picture update
                await _activityLogService.LogActivityAsync(
                    description: "Updated profile picture",
                    module: "Profile",
                    action: "Update",
                    userId: currentUser.Id
                );

                TempData["SuccessMessage"] = "Profile picture updated successfully.";
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        public async Task<IActionResult> CreateServiceRequest([FromBody] ServiceRequestViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Log the service request
            await _activityLogService.LogActivityAsync(
                description: $"Created service request: {model.Title}",
                module: "Service",
                action: "Create",
                userId: currentUser.Id,
                status: "Pending"
            );

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> CreateForumPost([FromBody] ForumPostViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Log the forum post
            await _activityLogService.LogActivityAsync(
                description: $"Created forum post: {model.Title}",
                module: "Forum",
                action: "Create",
                userId: currentUser.Id
            );

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterVehicle([FromBody] VehicleRegistrationViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            // Log the vehicle registration
            await _activityLogService.LogActivityAsync(
                description: $"Registered vehicle: {model.PlateNumber}",
                module: "Security",
                action: "Register",
                userId: currentUser.Id,
                status: "Pending"
            );

            return Ok();
        }
    }

    public class ServiceRequestViewModel
    {
        public required string Title { get; set; }
        public required string Description { get; set; }
        public required string Category { get; set; }
        public string? Priority { get; set; }
    }

    public class ForumPostViewModel
    {
        public required string Title { get; set; }
        public required string Content { get; set; }
        public required string Category { get; set; }
    }

    public class VehicleRegistrationViewModel
    {
        public required string VehicleType { get; set; }
        public required string MakeModel { get; set; }
        public required string PlateNumber { get; set; }
        public required string Color { get; set; }
    }
} 
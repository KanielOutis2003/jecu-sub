using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Data;
using SubdivisionWebsite.Models;
using SubdivisionWebsite.ViewModels;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SubdivisionWebsite.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly AppDbContext _context;
        protected readonly UserManager<ApplicationUser> _userManager;
        protected readonly IWebHostEnvironment? _webHostEnvironment;

        public BaseController(
            AppDbContext context, 
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public BaseController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = null;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (User?.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    try
                    {
                        var unreadCount = await _context.Set<Announcement>()
                            .Where(a => !_context.Set<AnnouncementRead>()
                                .Any(ar => ar.AnnouncementId == a.Id && ar.UserId == user.Id))
                            .CountAsync();

                        var recentAnnouncements = await _context.Set<Announcement>()
                            .Include(a => a.CreatedBy)
                            .OrderByDescending(a => a.CreatedAt)
                            .Take(5)
                            .ToListAsync();

                        ViewData["UnreadAnnouncements"] = unreadCount;
                        ViewData["RecentAnnouncements"] = recentAnnouncements;
                    }
                    catch (Exception ex)
                    {
                        // Log the error but don't throw it to prevent breaking the page
                        Console.WriteLine($"Error loading announcements: {ex.Message}");
                        ViewData["UnreadAnnouncements"] = 0;
                        ViewData["RecentAnnouncements"] = new List<Announcement>();
                    }
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                Address = user.Address,
                PhoneNumber = user.PhoneNumber,
                ExistingProfilePicture = user.ProfilePicture
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Address = model.Address;
                user.PhoneNumber = model.PhoneNumber;

                // Handle profile picture upload
                if (model.ProfilePicture != null && model.ProfilePicture.Length > 0 && _webHostEnvironment != null)
                {
                    // Create uploads directory if it doesn't exist
                    var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Generate unique filename
                    var uniqueFileName = $"{user.Id}_{DateTime.Now.Ticks}{Path.GetExtension(model.ProfilePicture.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save the file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfilePicture.CopyToAsync(fileStream);
                    }

                    // Update user profile picture path
                    user.ProfilePicture = $"/uploads/profiles/{uniqueFileName}";
                }

                // Update email if changed (requires additional steps)
                if (!string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase))
                {
                    var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        var errorDescription = setEmailResult.Errors.FirstOrDefault()?.Description ?? "Failed to update email";
                        ModelState.AddModelError(string.Empty, errorDescription);
                        return View(model);
                    }
                    
                    // Update username to match email
                    user.UserName = model.Email;
                }

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Your profile has been updated successfully.";
                    return RedirectToAction(nameof(Profile));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }
    }
} 
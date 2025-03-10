using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Data;
using SubdivisionWebsite.Models;
using SubdivisionWebsite.ViewModels;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading.Tasks;
using SubdivisionWebsite.Services;
using System.Linq;
using System.Collections.Generic;

namespace SubdivisionWebsite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        private readonly IActivityLogService _activityLogService;

        public AdminController(
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
            
            // Get recent activities for the dashboard
            ViewBag.RecentActivities = await _activityLogService.GetRecentActivitiesAsync(10);

            // Get counts for the overview cards
            ViewBag.TotalHomeowners = await _userManager.GetUsersInRoleAsync("Homeowner");
            ViewBag.ActiveStaff = await _userManager.GetUsersInRoleAsync("Staff");
            ViewBag.ActiveRequests = await GetActiveRequestsCount();
            ViewBag.PendingPayments = await GetPendingPaymentsCount();
            
            try {
                ViewBag.AnnouncementsCount = 0;
                if (_context.Model.FindEntityType(typeof(Announcement)) != null) {
                    ViewBag.AnnouncementsCount = await _context.Set<Announcement>().CountAsync();
                }
            }
            catch (Exception ex) {
                // Log the error but don't throw it
                Console.WriteLine($"Error getting announcements count: {ex.Message}");
            }
            
            try {
                ViewBag.FacilitiesCount = 0;
                if (_context.Model.FindEntityType(typeof(Facility)) != null) {
                    ViewBag.FacilitiesCount = await _context.Set<Facility>().CountAsync();
                }
            }
            catch (Exception ex) {
                // Log the error but don't throw it
                Console.WriteLine($"Error getting facilities count: {ex.Message}");
            }
            
            try {
                ViewBag.PendingReservationsCount = 0;
                if (_context.Model.FindEntityType(typeof(FacilityReservation)) != null) {
                    ViewBag.PendingReservationsCount = await _context.Set<FacilityReservation>()
                        .Where(r => r.Status == ReservationStatus.Pending)
                        .CountAsync();
                }
            }
            catch (Exception ex) {
                // Log the error but don't throw it
                Console.WriteLine($"Error getting pending reservations count: {ex.Message}");
            }
            
            return View();
        }

        public async Task<IActionResult> Reports()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            // Get statistics for different modules
            ViewBag.ServiceStats = await _activityLogService.GetModuleStatisticsAsync("Service", thirtyDaysAgo, null);
            ViewBag.BillingStats = await _activityLogService.GetModuleStatisticsAsync("Billing", thirtyDaysAgo, null);
            ViewBag.SecurityStats = await _activityLogService.GetModuleStatisticsAsync("Security", thirtyDaysAgo, null);
            ViewBag.ForumStats = await _activityLogService.GetModuleStatisticsAsync("Forum", thirtyDaysAgo, null);

            // Get recent activities for each module
            ViewBag.ServiceActivities = await _activityLogService.GetModuleActivitiesAsync("Service", thirtyDaysAgo, null);
            ViewBag.BillingActivities = await _activityLogService.GetModuleActivitiesAsync("Billing", thirtyDaysAgo, null);
            ViewBag.SecurityActivities = await _activityLogService.GetModuleActivitiesAsync("Security", thirtyDaysAgo, null);
            ViewBag.ForumActivities = await _activityLogService.GetModuleActivitiesAsync("Forum", thirtyDaysAgo, null);

            return View();
        }

        public async Task<IActionResult> Documents()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Log the document access
            await _activityLogService.LogActivityAsync(
                description: "Accessed document management",
                module: "Documents",
                action: "Access",
                userId: currentUser.Id
            );

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> LogActivity([FromBody] ActivityLogRequest request)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Unauthorized();
            }

            await _activityLogService.LogActivityAsync(
                description: request.Description,
                module: request.Module,
                action: request.Action,
                userId: currentUser.Id,
                status: request.Status,
                relatedEntityId: request.RelatedEntityId,
                relatedEntityType: request.RelatedEntityType
            );

            return Ok();
        }

        private async Task<int> GetActiveRequestsCount()
        {
            return await _context.Set<ActivityLog>()
                .Where(a => a.Module == "Service" && a.Status == "In Progress")
                .CountAsync();
        }

        private async Task<int> GetPendingPaymentsCount()
        {
            return await _context.Set<ActivityLog>()
                .Where(a => a.Module == "Billing" && a.Status == "Pending")
                .CountAsync();
        }

        public async Task<IActionResult> HomeownersList()
        {
            var homeowners = await _userManager.GetUsersInRoleAsync("Homeowner");
            return View(homeowners);
        }

        public async Task<IActionResult> StaffList()
        {
            var staffMembers = await _userManager.GetUsersInRoleAsync("Staff");
            return View(staffMembers);
        }

        [HttpGet]
        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Set<Announcement>()
                .Include(a => a.CreatedBy)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(announcements);
        }

        [HttpGet]
        public IActionResult CreateAnnouncement()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncement(AnnouncementViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var announcement = new Announcement
                {
                    Title = model.Title,
                    Content = model.Content,
                    CreatedById = user.Id
                };

                _context.Set<Announcement>().Add(announcement);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Announcement created successfully!";
                return RedirectToAction(nameof(Announcements));
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAnnouncement(int id)
        {
            var announcement = await _context.Set<Announcement>().FindAsync(id);
            if (announcement != null)
            {
                _context.Set<Announcement>().Remove(announcement);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Announcement deleted successfully!";
            }

            return RedirectToAction(nameof(Announcements));
        }

        public IActionResult Facilities()
        {
            var facilities = _context.Set<Facility>()
                .OrderBy(f => f.Name)
                .ToList();
            return View(facilities);
        }

        public IActionResult Events()
        {
            var events = _context.Set<Event>()
                .Include(e => e.CreatedBy)
                .OrderByDescending(e => e.StartDate)
                .ToList();
            return View(events);
        }

        public IActionResult Settings()
        {
            return View();
        }

        [HttpGet]
        public IActionResult RegisterStaff()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegisterStaff(StaffRegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var staff = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber,
                    LotNumber = "N/A",
                    BlockNumber = "N/A",
                    UserType = UserType.Staff,
                    StaffRole = model.StaffRole,
                    ProfilePicture = "default-profile.png",
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(staff, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(staff, "Staff");
                    TempData["SuccessMessage"] = "Staff account created successfully!";
                    return RedirectToAction(nameof(StaffList));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        public IActionResult AddStaff()
        {
            return RedirectToAction(nameof(RegisterStaff));
        }

        [HttpGet]
        public async Task<IActionResult> EditStaff(string id)
        {
            var staff = await _userManager.FindByIdAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            var model = new EditStaffViewModel
            {
                Id = staff.Id,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = staff.Email ?? string.Empty,
                Address = staff.Address,
                PhoneNumber = staff.PhoneNumber ?? string.Empty,
                StaffRole = staff.StaffRole ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EditStaff(EditStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                var staff = await _userManager.FindByIdAsync(model.Id);
                if (staff == null)
                {
                    return NotFound();
                }

                staff.FirstName = model.FirstName;
                staff.LastName = model.LastName;
                staff.Email = model.Email;
                staff.UserName = model.Email;
                staff.Address = model.Address;
                staff.PhoneNumber = model.PhoneNumber;
                staff.StaffRole = model.StaffRole;

                var result = await _userManager.UpdateAsync(staff);
                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Staff member updated successfully!";
                    return RedirectToAction(nameof(StaffList));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteStaff(string id)
        {
            var staff = await _userManager.FindByIdAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(staff);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Staff member deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Error deleting staff member.";
            }

            return RedirectToAction(nameof(StaffList));
        }

        // New modules
        public IActionResult Notifications()
        {
            return View();
        }

        public IActionResult CommunityForum()
        {
            return View();
        }

        public IActionResult ServiceRequests()
        {
            return View();
        }

        public IActionResult BillingPayments()
        {
            return View();
        }

        public IActionResult SecurityFeatures()
        {
            return View();
        }

        public IActionResult SystemSettings()
        {
            return View();
        }
    }

    public class ActivityLogRequest
    {
        public required string Description { get; set; }
        public required string Module { get; set; }
        public required string Action { get; set; }
        public required string Status { get; set; } = "Pending";
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
    }
} 
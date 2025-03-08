using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Data;
using SubdivisionWebsite.Models;
using SubdivisionWebsite.ViewModels;
using Microsoft.AspNetCore.Hosting;

namespace SubdivisionWebsite.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        public AdminController(
            UserManager<ApplicationUser> userManager, 
            AppDbContext context,
            IWebHostEnvironment webHostEnvironment)
            : base(context, userManager, webHostEnvironment)
        {
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalHomeowners = await _userManager.GetUsersInRoleAsync("Homeowner");
            ViewBag.ActiveStaff = await _userManager.GetUsersInRoleAsync("Staff");
            
            try {
                ViewBag.AnnouncementsCount = 0;
                if (_context.Model.FindEntityType(typeof(Announcement)) != null) {
                    ViewBag.AnnouncementsCount = await _context.Set<Announcement>().CountAsync();
                }
            }
            catch {
                ViewBag.AnnouncementsCount = 0;
            }
            
            try {
                ViewBag.FacilitiesCount = 0;
                if (_context.Model.FindEntityType(typeof(Facility)) != null) {
                    ViewBag.FacilitiesCount = await _context.Set<Facility>().CountAsync();
                }
            }
            catch {
                ViewBag.FacilitiesCount = 0;
            }
            
            try {
                ViewBag.PendingReservationsCount = 0;
                if (_context.Model.FindEntityType(typeof(FacilityReservation)) != null) {
                    ViewBag.PendingReservationsCount = await _context.Set<FacilityReservation>()
                        .Where(r => r.Status == ReservationStatus.Pending)
                        .CountAsync();
                }
            }
            catch {
                ViewBag.PendingReservationsCount = 0;
            }
            
            return View();
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
            return View();
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
    }
} 
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SubdivisionWebsite.Models;
using Microsoft.AspNetCore.Hosting;
using SubdivisionWebsite.Data;
using Microsoft.EntityFrameworkCore;

namespace SubdivisionWebsite.Controllers
{
    [Authorize(Roles = "Staff")]
    public class StaffController : BaseController
    {
        public StaffController(
            UserManager<ApplicationUser> userManager, 
            AppDbContext context,
            IWebHostEnvironment webHostEnvironment)
            : base(context, userManager, webHostEnvironment)
        {
        }

        public async Task<IActionResult> Dashboard()
        {
            // Get counts for the dashboard
            ViewBag.TotalHomeowners = await _userManager.GetUsersInRoleAsync("Homeowner");
            ViewBag.AnnouncementsCount = 0; // You can update this when you implement announcements
            ViewBag.FacilitiesCount = 0;    // You can update this when you implement facilities
            
            return View();
        }

        public async Task<IActionResult> HomeownersList()
        {
            var homeowners = await _userManager.GetUsersInRoleAsync("Homeowner");
            return View(homeowners);
        }

        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements
                .Include(a => a.CreatedBy)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
            return View(announcements);
        }

        public async Task<IActionResult> Facilities()
        {
            var facilities = await _context.Facilities
                .OrderBy(f => f.Name)
                .ToListAsync();
            return View(facilities);
        }

        // Use new keyword to explicitly hide the base method and await the async call
        public new async Task<IActionResult> Profile()
        {
            return await base.Profile();
        }
    }
} 
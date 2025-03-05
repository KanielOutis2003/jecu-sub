using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using SubdivisionWebsite.Models;
using Microsoft.AspNetCore.Hosting;
using SubdivisionWebsite.Data;

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

        public IActionResult HomeownersList()
        {
            return View();
        }

        public IActionResult Announcements()
        {
            return View();
        }

        public IActionResult Facilities()
        {
            return View();
        }

        // Use new keyword to explicitly hide the base method and await the async call
        public new async Task<IActionResult> Profile()
        {
            return await base.Profile();
        }
    }
} 
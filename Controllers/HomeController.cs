using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SubdivisionWebsite.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using System.Threading.Tasks;
using SubdivisionWebsite.Data;

namespace SubdivisionWebsite.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(ILogger<HomeController> logger, AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    public IActionResult Index()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> Announcements()
    {
        var announcements = await _context.Announcements
            .Include(a => a.CreatedBy)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = _userManager.GetUserId(User);
            ViewBag.ReadAnnouncements = await _context.AnnouncementReads
                .Where(ar => ar.UserId == userId)
                .Select(ar => ar.AnnouncementId)
                .ToListAsync();
        }

        return View(announcements);
    }

    [Authorize]
    [HttpGet]
    public async Task<JsonResult> GetNewAnnouncements()
    {
        if (User.Identity?.IsAuthenticated != true || !User.IsInRole("Homeowner"))
        {
            return Json(new object[0]);
        }

        var userId = _userManager.GetUserId(User);
        
        // Get announcements that the user hasn't read yet
        var unreadAnnouncements = await _context.Announcements
            .Where(a => a.IsActive && !_context.AnnouncementReads.Any(ar => ar.AnnouncementId == a.Id && ar.UserId == userId))
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.Title,
                a.Content,
                a.CreatedAt,
                CreatedBy = a.CreatedBy != null ? a.CreatedBy.FirstName + " " + a.CreatedBy.LastName : "Unknown"
            })
            .ToListAsync();

        return Json(unreadAnnouncements);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAnnouncementsAsRead()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized();
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized();
        }
        
        // Get all unread announcements
        var unreadAnnouncementIds = await _context.Announcements
            .Where(a => !_context.AnnouncementReads.Any(ar => ar.AnnouncementId == a.Id && ar.UserId == userId))
            .Select(a => a.Id)
            .ToListAsync();

        // Mark them as read
        foreach (var announcementId in unreadAnnouncementIds)
        {
            _context.AnnouncementReads.Add(new AnnouncementRead
            {
                AnnouncementId = announcementId,
                UserId = userId,
                ReadAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult AboutUs()
    {
        return View();
    }

    [Authorize]
    public async Task<IActionResult> CommunityForum()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Log activity if needed
        // You can add activity logging here if you have the service

        return View();
    }

    [Authorize]
    public async Task<IActionResult> SecurityFeatures()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Log activity if needed
        // You can add activity logging here if you have the service

        return View();
    }

    [Authorize]
    public async Task<IActionResult> ServiceRequests()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Log activity if needed
        // You can add activity logging here if you have the service

        return View();
    }

    // GET: Home/AdminDashboard
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AdminDashboard()
    {
        // Get existing dashboard data
        
        // Add pending reservations data
        var pendingReservations = await _context.FacilityReservations
            .Where(r => r.Status == ReservationStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.Facility)
            .Include(r => r.User)
            .Take(5) // Show only the 5 most recent pending reservations
            .ToListAsync();
            
        ViewBag.PendingReservations = pendingReservations;
        
        // Continue with the rest of the dashboard data
        
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SubdivisionWebsite.Controllers
{
    public class FacilityController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public FacilityController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Facility
        public async Task<IActionResult> Index()
        {
            var facilities = await _context.Facilities
                .Where(f => f.IsActive)
                .ToListAsync();

            return View(facilities);
        }

        // GET: Facility/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var facility = await _context.Facilities
                .FirstOrDefaultAsync(m => m.Id == id);

            if (facility == null)
            {
                return NotFound();
            }

            return View(facility);
        }

        // GET: Facility/Create
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Create()
        {
            // Provide a list of common facility types for selection
            ViewBag.FacilityTypes = new List<string>
            {
                "Community Center",
                "Swimming Pool",
                "Exercise Facility",
                "Playground",
                "Dog Park",
                "Park/Green Space",
                "Walking/Jogging Trail",
                "Basketball Court",
                "Tennis Court",
                "Main Street Village Center",
                "Outdoor Resort-style Pool",
                "Function Hall",
                "Sports Court",
                "Other"
            };
            
            return View();
        }

        // POST: Facility/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create(Facility facility, IFormFile facilityImage)
        {
            // Provide the list again in case we need to return to the view
            ViewBag.FacilityTypes = new List<string>
            {
                "Community Center",
                "Swimming Pool",
                "Exercise Facility",
                "Playground",
                "Dog Park",
                "Park/Green Space",
                "Walking/Jogging Trail",
                "Basketball Court",
                "Tennis Court",
                "Main Street Village Center",
                "Outdoor Resort-style Pool",
                "Function Hall",
                "Sports Court",
                "Other"
            };
            
            try
            {
                if (ModelState.IsValid)
                {
                    // Handle image upload if provided
                    if (facilityImage != null && facilityImage.Length > 0)
                    {
                        // Check if the uploads directory exists, if not create it
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "facilities");
                        if (!Directory.Exists(uploadsFolder))
                        {
                            Directory.CreateDirectory(uploadsFolder);
                        }

                        // Generate unique filename
                        var uniqueFileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(facilityImage.FileName)}";
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        // Save the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await facilityImage.CopyToAsync(fileStream);
                        }

                        // Set the image URL
                        facility.ImageUrl = $"/uploads/facilities/{uniqueFileName}";
                    }
                    else
                    {
                        // Set a default image based on facility type
                        facility.ImageUrl = GetDefaultFacilityImage(facility.Name);
                    }

                    // Ensure TimeSpan values are set properly
                    if (facility.OpeningTime == default)
                    {
                        facility.OpeningTime = new TimeSpan(8, 0, 0); // Default to 8:00 AM
                    }
                    
                    if (facility.ClosingTime == default)
                    {
                        facility.ClosingTime = new TimeSpan(22, 0, 0); // Default to 10:00 PM
                    }

                    _context.Add(facility);
                    await _context.SaveChangesAsync();
                    
                    // Create notification for all users about the new facility
                    await CreateFacilityNotification(facility);
                    
                    // Add success message
                    TempData["SuccessMessage"] = $"The facility '{facility.Name}' has been created successfully!";
                    
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating facility: {ex.Message}");
            }
            
            return View(facility);
        }

        // GET: Facility/Edit/5
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null)
            {
                return NotFound();
            }
            return View(facility);
        }

        // POST: Facility/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int id, Facility facility)
        {
            if (id != facility.Id)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // Ensure TimeSpan values are set properly
                    if (facility.OpeningTime == default)
                    {
                        facility.OpeningTime = new TimeSpan(8, 0, 0); // Default to 8:00 AM
                    }
                    
                    if (facility.ClosingTime == default)
                    {
                        facility.ClosingTime = new TimeSpan(22, 0, 0); // Default to 10:00 PM
                    }
                    
                    _context.Update(facility);
                    await _context.SaveChangesAsync();
                    
                    // Add success message
                    TempData["SuccessMessage"] = $"The facility '{facility.Name}' has been updated successfully!";
                    
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FacilityExists(facility.Id))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError("", "The facility was modified by another user. Please try again.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating facility: {ex.Message}");
            }
            
            return View(facility);
        }

        // GET: Facility/Reserve/5
        [Authorize]
        public async Task<IActionResult> Reserve(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var facility = await _context.Facilities
                .FirstOrDefaultAsync(m => m.Id == id && m.IsActive);

            if (facility == null)
            {
                return NotFound();
            }

            // Create a placeholder reservation with default values
            var reservation = new FacilityReservation
            {
                FacilityId = facility.Id,
                ReservationDate = DateTime.Today.AddDays(1),
                StartTime = facility.OpeningTime,
                EndTime = facility.OpeningTime.Add(new TimeSpan(2, 0, 0)), // Default 2 hours
                UserId = GetCurrentUserId(), // Will be set properly in POST action
                Purpose = "Please specify your purpose here" // Placeholder text
            };

            ViewBag.Facility = facility;
            return View(reservation);
        }

        // POST: Facility/Reserve
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Reserve(FacilityReservation reservation)
        {
            var facility = await _context.Facilities.FindAsync(reservation.FacilityId);
            
            if (facility == null)
            {
                return NotFound();
            }

            ViewBag.Facility = facility;

            if (ModelState.IsValid)
            {
                // Check if the facility is available for the requested time
                bool isAvailable = await IsFacilityAvailable(
                    reservation.FacilityId,
                    reservation.ReservationDate,
                    reservation.StartTime,
                    reservation.EndTime);

                if (!isAvailable)
                {
                    ModelState.AddModelError("", "The facility is not available for the selected time.");
                    return View(reservation);
                }

                // Set the user ID
                reservation.UserId = GetCurrentUserId();
                reservation.CreatedAt = DateTime.UtcNow;
                reservation.Status = ReservationStatus.Pending;

                _context.Add(reservation);
                await _context.SaveChangesAsync();

                // Create notification for admin/staff
                await CreateReservationNotification(reservation);
                
                // Add success message
                TempData["SuccessMessage"] = $"Your reservation request for {facility.Name} has been submitted successfully and is pending approval.";

                return RedirectToAction(nameof(MyReservations));
            }

            return View(reservation);
        }

        // GET: Facility/MyReservations
        [Authorize]
        public async Task<IActionResult> MyReservations()
        {
            var userId = GetCurrentUserId();
            var reservations = await _context.FacilityReservations
                .Include(r => r.Facility)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(reservations);
        }

        // GET: Facility/ManageReservations
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ManageReservations(string? status = null, int? facilityId = null, DateTime? date = null)
        {
            // Store filter values for the view
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentFacilityId = facilityId;
            ViewBag.CurrentDate = date?.ToString("yyyy-MM-dd");
            
            // Get all facilities for the dropdown
            ViewBag.Facilities = await _context.Facilities.OrderBy(f => f.Name).ToListAsync();
            
            // Start with all reservations
            var reservationsQuery = _context.FacilityReservations
                .Include(r => r.Facility)
                .Include(r => r.User)
                .AsQueryable();
            
            // Apply filters if provided
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<ReservationStatus>(status, out var statusEnum))
                {
                    reservationsQuery = reservationsQuery.Where(r => r.Status == statusEnum);
                }
            }
            
            if (facilityId.HasValue)
            {
                reservationsQuery = reservationsQuery.Where(r => r.FacilityId == facilityId.Value);
            }
            
            if (date.HasValue)
            {
                reservationsQuery = reservationsQuery.Where(r => r.ReservationDate.Date == date.Value.Date);
            }
            
            // Get the final list of reservations
            var reservations = await reservationsQuery
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            
            return View(reservations);
        }

        // POST: Facility/ApproveReservation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ApproveReservation(int id)
        {
            var reservation = await _context.FacilityReservations
                .Include(r => r.User)
                .Include(r => r.Facility)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.Status = ReservationStatus.Approved;
            reservation.ApprovedById = GetCurrentUserId();
            reservation.ApprovedAt = DateTime.UtcNow;

            _context.Update(reservation);
            await _context.SaveChangesAsync();

            // Create notification for the user
            await CreateReservationStatusNotification(reservation);
            
            // Add success message
            TempData["SuccessMessage"] = $"The reservation for {reservation.Facility?.Name ?? "the facility"} has been approved successfully!";

            return RedirectToAction(nameof(ManageReservations));
        }

        // POST: Facility/RejectReservation/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> RejectReservation(int id, string rejectionReason)
        {
            var reservation = await _context.FacilityReservations
                .Include(r => r.User)
                .Include(r => r.Facility)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.Status = ReservationStatus.Rejected;
            reservation.RejectionReason = rejectionReason;
            reservation.ApprovedById = GetCurrentUserId();
            reservation.ApprovedAt = DateTime.UtcNow;

            _context.Update(reservation);
            await _context.SaveChangesAsync();

            // Create notification for the user
            await CreateReservationStatusNotification(reservation);
            
            // Add success message
            TempData["SuccessMessage"] = $"The reservation for {reservation.Facility?.Name ?? "the facility"} has been rejected.";

            return RedirectToAction(nameof(ManageReservations));
        }

        // Helper methods
        private bool FacilityExists(int id)
        {
            return _context.Facilities.Any(e => e.Id == id);
        }

        private async Task<bool> IsFacilityAvailable(int facilityId, DateTime date, TimeSpan startTime, TimeSpan endTime)
        {
            // Check if there are any approved reservations that overlap with the requested time
            var overlappingReservations = await _context.FacilityReservations
                .Where(r => r.FacilityId == facilityId &&
                            r.ReservationDate == date &&
                            r.Status == ReservationStatus.Approved &&
                            ((r.StartTime <= startTime && r.EndTime > startTime) ||
                             (r.StartTime < endTime && r.EndTime >= endTime) ||
                             (r.StartTime >= startTime && r.EndTime <= endTime)))
                .AnyAsync();

            return !overlappingReservations;
        }

        private async Task CreateReservationNotification(FacilityReservation reservation)
        {
            // Create notification for admins and staff
            var adminStaffUsers = await _userManager.GetUsersInRoleAsync("Admin");
            adminStaffUsers = adminStaffUsers.Concat(await _userManager.GetUsersInRoleAsync("Staff")).ToList();

            foreach (var user in adminStaffUsers)
            {
                var notification = new Notification
                {
                    Title = "New Facility Reservation",
                    Message = $"A new reservation request for {reservation.Facility?.Name ?? "a facility"} has been submitted.",
                    UserId = user.Id,
                    Type = NotificationType.FacilityReservation,
                    ReferenceId = reservation.Id,
                    ActionUrl = "/Facility/ManageReservations"
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        private async Task CreateReservationStatusNotification(FacilityReservation reservation)
        {
            var statusText = reservation.Status == ReservationStatus.Approved ? "approved" : "rejected";
            var message = reservation.Status == ReservationStatus.Approved
                ? $"Your reservation for {reservation.Facility?.Name ?? "the facility"} on {reservation.ReservationDate.ToShortDateString()} has been approved."
                : $"Your reservation for {reservation.Facility?.Name ?? "the facility"} on {reservation.ReservationDate.ToShortDateString()} has been rejected. Reason: {reservation.RejectionReason}";

            var notification = new Notification
            {
                Title = $"Reservation {statusText.ToUpper()}",
                Message = message,
                UserId = reservation.UserId,
                Type = NotificationType.FacilityReservation,
                ReferenceId = reservation.Id,
                ActionUrl = "/Facility/MyReservations"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        // Get the current user ID
        private string GetCurrentUserId()
        {
            return _userManager.GetUserId(User) ?? throw new InvalidOperationException("User is not authenticated");
        }

        // Helper method to get default image based on facility type
        private string GetDefaultFacilityImage(string facilityName)
        {
            facilityName = facilityName.ToLower();
            
            if (facilityName.Contains("pool") || facilityName.Contains("swim"))
                return "/images/facilities/default-pool.jpg";
            else if (facilityName.Contains("gym") || facilityName.Contains("exercise") || facilityName.Contains("fitness"))
                return "/images/facilities/default-gym.jpg";
            else if (facilityName.Contains("park") || facilityName.Contains("garden") || facilityName.Contains("green"))
                return "/images/facilities/default-park.jpg";
            else if (facilityName.Contains("playground") || facilityName.Contains("play"))
                return "/images/facilities/default-playground.jpg";
            else if (facilityName.Contains("court") || facilityName.Contains("sport") || facilityName.Contains("basketball") || facilityName.Contains("tennis"))
                return "/images/facilities/default-court.jpg";
            else if (facilityName.Contains("hall") || facilityName.Contains("function") || facilityName.Contains("community") || facilityName.Contains("center"))
                return "/images/facilities/default-hall.jpg";
            else if (facilityName.Contains("trail") || facilityName.Contains("walk") || facilityName.Contains("jog"))
                return "/images/facilities/default-trail.jpg";
            else
                return "/images/facilities/default-facility.jpg";
        }

        private async Task CreateFacilityNotification(Facility facility)
        {
            // Create notification for all users
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var notification = new Notification
                {
                    Title = "New Facility Available",
                    Message = $"A new facility '{facility.Name}' is now available for reservation.",
                    UserId = user.Id,
                    Type = NotificationType.System,
                    ReferenceId = facility.Id,
                    ActionUrl = $"/Facility/Details/{facility.Id}"
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }
    }
} 
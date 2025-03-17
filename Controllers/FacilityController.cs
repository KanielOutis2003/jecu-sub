using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Models;
using SubdivisionWebsite.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace SubdivisionWebsite.Controllers
{
    [Authorize]
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
            
            // Log the received facility data for debugging
            System.Diagnostics.Debug.WriteLine($"Received Facility: Name={facility.Name}, Description={facility.Description?.Length ?? 0} chars, Location={facility.Location}");
            System.Diagnostics.Debug.WriteLine($"OpeningTime={facility.OpeningTime}, ClosingTime={facility.ClosingTime}, Capacity={facility.Capacity}, ReservationFee={facility.ReservationFee}");
            
            // Validate required fields explicitly
            if (string.IsNullOrWhiteSpace(facility.Name))
            {
                ModelState.AddModelError("Name", "Facility name is required");
                System.Diagnostics.Debug.WriteLine("Validation Error: Missing Name");
            }
            
            if (string.IsNullOrWhiteSpace(facility.Description))
            {
                ModelState.AddModelError("Description", "Description is required");
                System.Diagnostics.Debug.WriteLine("Validation Error: Missing Description");
            }
            
            if (string.IsNullOrWhiteSpace(facility.Location))
            {
                ModelState.AddModelError("Location", "Location is required");
                System.Diagnostics.Debug.WriteLine("Validation Error: Missing Location");
            }
            
            // Log all model state errors
            if (!ModelState.IsValid)
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"Model Error: {error.ErrorMessage}");
                    }
                }
            }
            
            try
            {
                if (ModelState.IsValid)
                {
                    // Set a default image url if no image is uploaded
                    facility.ImageUrl = GetDefaultFacilityImage(facility.Name);
                    
                    // Handle image upload if provided
                    if (facilityImage != null && facilityImage.Length > 0)
                    {
                        // Check file size (limit to 5MB)
                        if (facilityImage.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("facilityImage", "Image size should not exceed 5MB");
                            return View(facility);
                        }
                        
                        // Check file extension
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                        var extension = Path.GetExtension(facilityImage.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("facilityImage", "Only image files (.jpg, .jpeg, .png, .gif) are allowed");
                            return View(facility);
                        }
                        
                        try
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
                        catch (Exception ex)
                        {
                            // Log the error but continue with default image
                            System.Diagnostics.Debug.WriteLine($"Error uploading image: {ex.Message}");
                            ModelState.AddModelError("facilityImage", $"Error uploading image: {ex.Message}. Using default image instead.");
                            // Don't return here, we'll continue with the default image
                        }
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
                    
                    // Validate that opening time is before closing time
                    if (facility.OpeningTime >= facility.ClosingTime)
                    {
                        ModelState.AddModelError("OpeningTime", "Opening time must be before closing time");
                        ModelState.AddModelError("ClosingTime", "Closing time must be after opening time");
                        return View(facility);
                    }
                    
                    // Validate capacity is reasonable
                    if (facility.Capacity <= 0)
                    {
                        facility.Capacity = 50; // Set a default value if not provided or invalid
                    }

                    // Save to database
                    try
                    {
                        System.Diagnostics.Debug.WriteLine("Attempting to add facility to database");
                        _context.Add(facility);
                        System.Diagnostics.Debug.WriteLine("Attempting to save changes to database");
                        await _context.SaveChangesAsync();
                        System.Diagnostics.Debug.WriteLine("Database save successful");
                        
                        // Create notification for all users about the new facility
                        await CreateFacilityNotification(facility);
                        
                        // Add success message
                        TempData["SuccessMessage"] = $"The facility '{facility.Name}' has been created successfully!";
                        
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateException dbEx)
                    {
                        var innerException = dbEx.InnerException?.Message ?? "No inner exception";
                        System.Diagnostics.Debug.WriteLine($"Database Update Error: {dbEx.Message}");
                        System.Diagnostics.Debug.WriteLine($"Inner Exception: {innerException}");
                        ModelState.AddModelError("", $"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}");
                        return View(facility);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                ModelState.AddModelError("", $"An unexpected error occurred: {ex.Message}");
                // Log the error
                Console.WriteLine($"Error creating facility: {ex}");
            }
            
            // If we got this far, something failed, redisplay form with errors
            System.Diagnostics.Debug.WriteLine("Failed to create facility - returning to form");
            TempData["ErrorMessage"] = "Failed to create facility. Please check the form and try again.";
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

        // GET: Facility/Delete/5
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Delete(int? id)
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

            try
            {
                // Check if there are any existing reservations for this facility
                bool hasReservations = await _context.FacilityReservations
                    .AnyAsync(r => r.FacilityId == id);
                    
                if (hasReservations)
                {
                    // Add warning message and redirect to Edit page
                    TempData["ErrorMessage"] = "This facility cannot be deleted because it has existing reservations. You can deactivate it instead.";
                    return RedirectToAction(nameof(Edit), new { id = id });
                }
                
                // If no reservations exist, delete the facility
                // Check if facility has an image that needs to be deleted
                if (!string.IsNullOrEmpty(facility.ImageUrl) && 
                    facility.ImageUrl.StartsWith("/uploads/facilities/"))
                {
                    try
                    {
                        var imagePath = Path.Combine(
                            _webHostEnvironment.WebRootPath, 
                            facility.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                            
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with deletion
                        System.Diagnostics.Debug.WriteLine($"Error deleting facility image: {ex.Message}");
                    }
                }
                
                _context.Facilities.Remove(facility);
                await _context.SaveChangesAsync();
                
                // Add success message
                TempData["SuccessMessage"] = $"The facility '{facility.Name}' has been deleted successfully!";
                
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Add error message and redirect to Edit page
                TempData["ErrorMessage"] = $"Error deleting facility: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id = id });
            }
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

            try 
            {
                // Additional validation
                if (reservation.ReservationDate < DateTime.Today.AddDays(1))
                {
                    ModelState.AddModelError("ReservationDate", "Reservations must be made at least 1 day in advance.");
                }

                if (reservation.StartTime < facility.OpeningTime)
                {
                    ModelState.AddModelError("StartTime", $"Start time must be after facility opening time ({facility.OpeningTime.ToString("hh\\:mm tt")}).");
                }

                if (reservation.EndTime > facility.ClosingTime)
                {
                    ModelState.AddModelError("EndTime", $"End time must be before facility closing time ({facility.ClosingTime.ToString("hh\\:mm tt")}).");
                }

                if (reservation.StartTime >= reservation.EndTime)
                {
                    ModelState.AddModelError("StartTime", "Start time must be before end time.");
                    ModelState.AddModelError("EndTime", "End time must be after start time.");
                }

                if (string.IsNullOrWhiteSpace(reservation.Purpose))
                {
                    ModelState.AddModelError("Purpose", "Purpose is required.");
                }

                // Check for maximum reservation duration (e.g., 8 hours)
                var duration = reservation.EndTime - reservation.StartTime;
                if (duration.TotalHours > 8)
                {
                    ModelState.AddModelError("EndTime", "Maximum reservation duration is 8 hours.");
                }

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
                        ModelState.AddModelError("", "The facility is not available for the selected time. Please choose a different time slot.");
                        return View(reservation);
                    }

                    // Set the user ID and other properties
                    reservation.UserId = GetCurrentUserId();
                    reservation.CreatedAt = DateTime.UtcNow;
                    reservation.Status = ReservationStatus.Pending;

                    _context.Add(reservation);
                    await _context.SaveChangesAsync();

                    // Log the activity
                    var activityLog = new ActivityLog
                    {
                        UserId = reservation.UserId,
                        Module = "Facility",
                        Action = "Reserve",
                        Description = $"Reserved {facility.Name} for {reservation.ReservationDate.ToShortDateString()} {reservation.StartTime.ToString("hh\\:mm tt")} - {reservation.EndTime.ToString("hh\\:mm tt")}",
                        Status = "Pending",
                        StatusColor = "warning",
                        RelatedEntityId = reservation.Id.ToString(),
                        RelatedEntityType = "FacilityReservation"
                    };
                    _context.ActivityLogs.Add(activityLog);
                    await _context.SaveChangesAsync();

                    // Create notification for admin/staff
                    await CreateReservationNotification(reservation);
                    
                    // Add success message
                    TempData["SuccessMessage"] = $"Your reservation request for {facility.Name} has been submitted successfully and is pending approval.";

                    return RedirectToAction(nameof(MyReservations));
                }
            }
            catch (Exception ex)
            {
                // Log the error
                System.Diagnostics.Debug.WriteLine($"Error creating reservation: {ex.Message}");
                ModelState.AddModelError("", $"An error occurred while creating your reservation: {ex.Message}");
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
        
        // GET: Facility/DiagnoseStorage
        [Authorize(Roles = "Admin")]
        public IActionResult DiagnoseStorage()
        {
            try
            {
                var model = new Dictionary<string, object>();
                
                // Check web root path
                model["WebRootPath"] = _webHostEnvironment.WebRootPath;
                model["WebRootExists"] = Directory.Exists(_webHostEnvironment.WebRootPath);
                
                // Check uploads directory
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                model["UploadsPath"] = uploadsPath;
                model["UploadsExists"] = Directory.Exists(uploadsPath);
                
                // Check facilities uploads directory
                var facilitiesUploadsPath = Path.Combine(uploadsPath, "facilities");
                model["FacilitiesUploadsPath"] = facilitiesUploadsPath;
                model["FacilitiesUploadsExists"] = Directory.Exists(facilitiesUploadsPath);
                
                // Check images directory
                var imagesPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
                model["ImagesPath"] = imagesPath;
                model["ImagesExists"] = Directory.Exists(imagesPath);
                
                // Check facilities images directory
                var facilitiesImagesPath = Path.Combine(imagesPath, "facilities");
                model["FacilitiesImagesPath"] = facilitiesImagesPath;
                model["FacilitiesImagesExists"] = Directory.Exists(facilitiesImagesPath);
                
                // Try to create a test file in facilities uploads directory
                var testFilePath = string.Empty;
                var canWriteToFacilitiesUploads = false;
                var writeErrorMessage = string.Empty;
                
                try
                {
                    if (!Directory.Exists(facilitiesUploadsPath))
                    {
                        Directory.CreateDirectory(facilitiesUploadsPath);
                    }
                    
                    testFilePath = Path.Combine(facilitiesUploadsPath, $"test_{DateTime.Now.Ticks}.txt");
                    System.IO.File.WriteAllText(testFilePath, "Test file for diagnostics");
                    canWriteToFacilitiesUploads = true;
                    
                    // Clean up
                    if (System.IO.File.Exists(testFilePath))
                    {
                        System.IO.File.Delete(testFilePath);
                    }
                }
                catch (Exception ex)
                {
                    writeErrorMessage = ex.Message;
                }
                
                model["CanWriteToFacilitiesUploads"] = canWriteToFacilitiesUploads;
                model["WriteErrorMessage"] = writeErrorMessage;
                
                return View(model);
            }
            catch (Exception ex)
            {
                return Content($"Diagnostics error: {ex.Message}");
            }
        }

        // GET: Facility/Debug
        [Authorize(Roles = "Admin")]
        public IActionResult Debug()
        {
            try
            {
                var model = new Dictionary<string, object>();
                
                // Get database connection details (without sensitive info)
                var connection = _context.Database.GetDbConnection();
                model["Database"] = connection.Database;
                model["DataSource"] = connection.DataSource;
                model["ConnectionState"] = connection.State.ToString();
                
                // Test database connection
                bool canConnect = false;
                string connectionError = string.Empty;
                try
                {
                    _context.Database.OpenConnection();
                    canConnect = true;
                    _context.Database.CloseConnection();
                }
                catch (Exception ex)
                {
                    connectionError = ex.Message;
                }
                model["CanConnect"] = canConnect;
                model["ConnectionError"] = connectionError;
                
                // Get Facility table info
                try
                {
                    // Check total count
                    model["FacilityCount"] = _context.Facilities.Count();
                    
                    // Get facility column names and types
                    var facilities = new List<Dictionary<string, string>>();
                    var facility = new Facility
                    {
                        Name = "[Debug] Test Facility",
                        Description = "This is a test facility for debugging",
                        Location = "Debug Location",
                        Capacity = 50,
                        IsActive = true,
                        OpeningTime = new TimeSpan(8, 0, 0),
                        ClosingTime = new TimeSpan(17, 0, 0),
                        ReservationFee = 0
                    };
                    
                    // Don't actually save, just get properties for debugging
                    var properties = typeof(Facility).GetProperties();
                    var propertyInfo = new List<Dictionary<string, string>>();
                    
                    foreach (var prop in properties)
                    {
                        var propInfo = new Dictionary<string, string>
                        {
                            ["Name"] = prop.Name,
                            ["Type"] = prop.PropertyType.ToString(),
                            ["Value"] = prop.GetValue(facility)?.ToString() ?? "null"
                        };
                        propertyInfo.Add(propInfo);
                    }
                    
                    model["FacilityProperties"] = propertyInfo;
                }
                catch (Exception ex)
                {
                    model["FacilityTableError"] = ex.Message;
                }
                
                return View(model);
            }
            catch (Exception ex)
            {
                return Content($"Debug error: {ex.Message}");
            }
        }

        // GET: Facility/SimpleFacilityTest
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SimpleFacilityTest()
        {
            try
            {
                // Create a simple facility with absolute bare minimum
                var facility = new Facility();
                
                // Start with null values
                System.Diagnostics.Debug.WriteLine("Simple Test - Created facility object with default values");
                
                // Set required values step by step
                facility.Name = "Test Facility " + DateTime.Now.ToString("yyyyMMddHHmmss");
                System.Diagnostics.Debug.WriteLine("Simple Test - Set Name");
                
                facility.Description = "This is a test facility created for debugging";
                System.Diagnostics.Debug.WriteLine("Simple Test - Set Description");
                
                facility.Location = "Test Location";
                System.Diagnostics.Debug.WriteLine("Simple Test - Set Location");
                
                facility.Capacity = 50;
                System.Diagnostics.Debug.WriteLine("Simple Test - Set Capacity");
                
                facility.IsActive = true;
                System.Diagnostics.Debug.WriteLine("Simple Test - Set IsActive");
                
                facility.OpeningTime = new TimeSpan(8, 0, 0);
                System.Diagnostics.Debug.WriteLine("Simple Test - Set OpeningTime");
                
                facility.ClosingTime = new TimeSpan(17, 0, 0);
                System.Diagnostics.Debug.WriteLine("Simple Test - Set ClosingTime");
                
                facility.ReservationFee = 0;
                System.Diagnostics.Debug.WriteLine("Simple Test - Set ReservationFee");
                
                System.Diagnostics.Debug.WriteLine($"Simple Test - Adding facility '{facility.Name}'");
                _context.Add(facility);
                System.Diagnostics.Debug.WriteLine("Simple Test - Saving to database");
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Simple Test - Save successful");
                
                return Content($"Test facility '{facility.Name}' created successfully with ID: {facility.Id}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Simple Test - Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Simple Test - Inner Error: {ex.InnerException.Message}");
                }
                
                return Content($"Error creating test facility: {ex.Message}\n\nInner exception: {ex.InnerException?.Message}");
            }
        }
    }
} 
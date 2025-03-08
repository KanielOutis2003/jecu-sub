using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubdivisionWebsite.Controllers
{
    public class EventController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Event
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.CreatedBy)
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();

            return View(events);
        }

        // GET: Event/Calendar
        public IActionResult Calendar()
        {
            return View();
        }

        // GET: Event/GetEvents
        [HttpGet]
        public async Task<JsonResult> GetEvents(DateTime start, DateTime end)
        {
            var events = await _context.Events
                .Where(e => (e.StartDate >= start && e.StartDate <= end) ||
                           (e.EndDate.HasValue && e.EndDate >= start && e.StartDate <= end))
                .Select(e => new
                {
                    id = e.Id,
                    title = e.Title,
                    start = e.StartDate.Add(e.StartTime),
                    end = e.EndDate.HasValue ? e.EndDate.Value.Add(e.EndTime ?? new TimeSpan(0, 0, 0)) : e.StartDate.Add(e.StartTime).AddHours(1),
                    description = e.Description,
                    location = e.Location,
                    allDay = e.IsAllDay,
                    color = e.Color ?? "#3788d8"
                })
                .ToListAsync();

            return Json(events);
        }

        // GET: Event/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events
                .Include(e => e.CreatedBy)
                .Include(e => e.Attendees)
                    .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            // Check if current user has responded to the event
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = GetCurrentUserId();
                ViewBag.UserResponse = @event.Attendees
                    .FirstOrDefault(a => a.UserId == userId)?.IsAttending;
            }

            return View(@event);
        }

        // GET: Event/Create
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Create()
        {
            // We'll set the CreatedById in the POST action
            var userId = GetCurrentUserId();
            
            var model = new Event
            {
                Title = "New Event",
                Description = "Event description goes here",
                StartDate = DateTime.Today,
                StartTime = new TimeSpan(9, 0, 0), // 9:00 AM
                EndDate = DateTime.Today,
                EndTime = new TimeSpan(10, 0, 0), // 10:00 AM
                Color = "#3788d8", // Default blue color
                CreatedById = userId // Required property
            };

            return View(model);
        }

        // POST: Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Create(Event @event)
        {
            if (ModelState.IsValid)
            {
                @event.CreatedById = GetCurrentUserId();
                @event.CreatedAt = DateTime.UtcNow;

                _context.Add(@event);
                await _context.SaveChangesAsync();

                // Create notifications for all users
                await CreateEventNotification(@event);

                // Add success message
                TempData["SuccessMessage"] = $"The event '{@event.Title}' has been created successfully!";

                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // GET: Event/Edit/5
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }
            return View(@event);
        }

        // POST: Event/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(int id, Event @event)
        {
            if (id != @event.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEvent = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
                    if (existingEvent == null)
                    {
                        return NotFound();
                    }

                    @event.CreatedById = existingEvent.CreatedById;
                    @event.CreatedAt = existingEvent.CreatedAt;
                    @event.LastUpdatedById = GetCurrentUserId();
                    @event.LastUpdatedAt = DateTime.UtcNow;

                    _context.Update(@event);
                    await _context.SaveChangesAsync();

                    // Create notifications for event update
                    await CreateEventUpdateNotification(@event);
                    
                    // Add success message
                    TempData["SuccessMessage"] = $"The event '{@event.Title}' has been updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EventExists(@event.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(@event);
        }

        // POST: Event/Respond/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Respond(int id, bool isAttending)
        {
            var @event = await _context.Events
                .Include(e => e.Attendees)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            var userId = GetCurrentUserId();
            var existingResponse = @event.Attendees.FirstOrDefault(a => a.UserId == userId);

            if (existingResponse != null)
            {
                // Update existing response
                existingResponse.IsAttending = isAttending;
                existingResponse.RespondedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new response
                var attendee = new EventAttendee
                {
                    EventId = id,
                    UserId = userId,
                    IsAttending = isAttending,
                    RespondedAt = DateTime.UtcNow
                };
                _context.EventAttendees.Add(attendee);
            }

            await _context.SaveChangesAsync();
            
            // Add success message
            var responseType = isAttending ? "attending" : "not attending";
            TempData["SuccessMessage"] = $"You have successfully marked yourself as {responseType} this event.";
            
            return RedirectToAction(nameof(Details), new { id });
        }

        // Helper methods
        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }

        private string GetCurrentUserId()
        {
            return _userManager.GetUserId(User) ?? throw new InvalidOperationException("User is not authenticated");
        }

        private async Task CreateEventNotification(Event @event)
        {
            // Create notification for all users
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var notification = new Notification
                {
                    Title = "New Event",
                    Message = $"A new event '{@event.Title}' has been scheduled for {@event.StartDate.ToShortDateString()}.",
                    UserId = user.Id,
                    Type = NotificationType.Event,
                    ReferenceId = @event.Id,
                    ActionUrl = $"/Event/Details/{@event.Id}"
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        private async Task CreateEventUpdateNotification(Event @event)
        {
            // Create notification for all users who have responded to the event
            var attendees = await _context.EventAttendees
                .Where(a => a.EventId == @event.Id)
                .Select(a => a.UserId)
                .ToListAsync();

            foreach (var userId in attendees)
            {
                var notification = new Notification
                {
                    Title = "Event Updated",
                    Message = $"The event '{@event.Title}' has been updated.",
                    UserId = userId,
                    Type = NotificationType.Event,
                    ReferenceId = @event.Id,
                    ActionUrl = $"/Event/Details/{@event.Id}"
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }
    }
} 
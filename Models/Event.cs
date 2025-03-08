using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        public required string Title { get; set; }

        [Required]
        public required string Description { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [DataType(DataType.Time)]
        public TimeSpan? EndTime { get; set; }

        public string? Location { get; set; }

        public bool IsAllDay { get; set; } = false;

        public string? Color { get; set; } // For calendar display

        [Required]
        public required string CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUpdatedAt { get; set; }

        public string? LastUpdatedById { get; set; }
        public ApplicationUser? LastUpdatedBy { get; set; }

        // For tracking RSVPs
        public ICollection<EventAttendee> Attendees { get; set; } = new List<EventAttendee>();
    }

    public class EventAttendee
    {
        public int EventId { get; set; }
        public Event? Event { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public bool IsAttending { get; set; }

        public DateTime RespondedAt { get; set; } = DateTime.UtcNow;
    }
} 
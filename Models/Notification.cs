using System;
using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.Models
{
    public enum NotificationType
    {
        Announcement,
        Event,
        FacilityReservation,
        Document,
        System
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public required string Title { get; set; }

        [Required]
        public required string Message { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public NotificationType Type { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        // Optional reference to related entity
        public int? ReferenceId { get; set; }

        // URL to navigate to when notification is clicked
        public string? ActionUrl { get; set; }

        // For email/SMS notifications
        public bool IsEmailSent { get; set; } = false;
        public DateTime? EmailSentAt { get; set; }

        public bool IsSMSSent { get; set; } = false;
        public DateTime? SMSSentAt { get; set; }
    }
} 
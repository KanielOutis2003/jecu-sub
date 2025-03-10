using System;
using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }

        [Required]
        public required string Description { get; set; }

        [Required]
        public required string Module { get; set; }  // e.g., "Billing", "Security", "Service", "Forum"

        [Required]
        public required string Action { get; set; }  // e.g., "Create", "Update", "Delete"

        [Required]
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public string? RelatedEntityId { get; set; }  // ID of the related entity (if applicable)
        public string? RelatedEntityType { get; set; }  // Type of the related entity

        [Required]
        public required string Status { get; set; } = "Pending";  // e.g., "Pending", "Completed", "Failed"
        
        [Required]
        public required string StatusColor { get; set; } = "secondary";  // Bootstrap color class for the status badge

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsVisible { get; set; } = true;  // For soft delete functionality
    }
} 
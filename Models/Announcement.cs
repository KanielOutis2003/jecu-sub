using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        public required string Title { get; set; }

        [Required]
        public required string Content { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public required string CreatedById { get; set; }
        public ApplicationUser? CreatedBy { get; set; }

        public bool IsActive { get; set; } = true;

        // For tracking who has read the announcement
        public ICollection<AnnouncementRead> ReadBy { get; set; } = new List<AnnouncementRead>();
    }

    public class AnnouncementRead
    {
        public int AnnouncementId { get; set; }
        public Announcement? Announcement { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
} 
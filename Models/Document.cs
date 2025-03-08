using System;
using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.Models
{
    public enum DocumentType
    {
        Form,
        Guideline,
        FinancialReport,
        MeetingMinutes,
        Other
    }

    public class Document
    {
        public int Id { get; set; }

        [Required]
        public required string Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public required string FilePath { get; set; }

        public string? FileType { get; set; }

        public long FileSize { get; set; }

        public DocumentType DocumentType { get; set; }

        public bool IsPublic { get; set; } = true;

        [Required]
        public required string UploadedById { get; set; }
        public ApplicationUser? UploadedBy { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastUpdatedAt { get; set; }

        public string? LastUpdatedById { get; set; }
        public ApplicationUser? LastUpdatedBy { get; set; }

        // Version tracking
        public int Version { get; set; } = 1;

        // For tracking document downloads
        public int DownloadCount { get; set; } = 0;
    }
} 
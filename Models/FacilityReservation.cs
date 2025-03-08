using System;
using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.Models
{
    public enum ReservationStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public class FacilityReservation
    {
        public int Id { get; set; }

        [Required]
        public int FacilityId { get; set; }
        public Facility? Facility { get; set; }

        [Required]
        public required string UserId { get; set; }
        public ApplicationUser? User { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime ReservationDate { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required]
        public required string Purpose { get; set; }

        public int? ExpectedAttendees { get; set; }

        public ReservationStatus Status { get; set; } = ReservationStatus.Pending;

        public string? RejectionReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? ApprovedById { get; set; }
        public ApplicationUser? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        // Payment information (if applicable)
        public bool IsPaid { get; set; } = false;
        public decimal? AmountPaid { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentReference { get; set; }
    }
} 
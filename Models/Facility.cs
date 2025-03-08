using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SubdivisionWebsite.Models
{
    public class Facility
    {
        public int Id { get; set; }

        [Required]
        public required string Name { get; set; }

        [Required]
        public required string Description { get; set; }

        [Required]
        public required string Location { get; set; }

        public int Capacity { get; set; }

        public bool IsActive { get; set; } = true;

        public string? ImageUrl { get; set; }

        // Opening and closing hours (24-hour format)
        public TimeSpan OpeningTime { get; set; } = new TimeSpan(8, 0, 0); // 8:00 AM
        public TimeSpan ClosingTime { get; set; } = new TimeSpan(22, 0, 0); // 10:00 PM

        // Reservation fee (if any)
        public decimal? ReservationFee { get; set; }

        // Navigation property for reservations
        public ICollection<FacilityReservation> Reservations { get; set; } = new List<FacilityReservation>();
    }
} 
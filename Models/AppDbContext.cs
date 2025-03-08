using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Models;

public class AppDbContext : IdentityDbContext<ApplicationUser> 
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Homeowner> Homeowners { get; set; } // Keep only additional entities
    public DbSet<Announcement> Announcements { get; set; }
    public DbSet<AnnouncementRead> AnnouncementReads { get; set; }
    public DbSet<Facility> Facilities { get; set; }
    public DbSet<FacilityReservation> FacilityReservations { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<EventAttendee> EventAttendees { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure AnnouncementRead composite key
        modelBuilder.Entity<AnnouncementRead>()
            .HasKey(ar => new { ar.AnnouncementId, ar.UserId });

        // Configure EventAttendee composite key
        modelBuilder.Entity<EventAttendee>()
            .HasKey(ea => new { ea.EventId, ea.UserId });
    }
}


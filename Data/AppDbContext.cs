using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SubdivisionWebsite.Models;

namespace SubdivisionWebsite.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Announcement> Announcements { get; set; } = null!;
        public DbSet<AnnouncementRead> AnnouncementReads { get; set; } = null!;
        public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;
        public DbSet<Facility> Facilities { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Document> Documents { get; set; } = null!;
        public DbSet<FacilityReservation> FacilityReservations { get; set; } = null!;
        public DbSet<EventAttendee> EventAttendees { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Configure identity tables for MySQL
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(85);
            });
            
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(85);
                entity.Property(e => e.NormalizedName).HasMaxLength(85);
            });
            
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(entity =>
            {
                entity.Property(e => e.LoginProvider).HasMaxLength(85);
                entity.Property(e => e.ProviderKey).HasMaxLength(85);
                entity.Property(e => e.UserId).HasMaxLength(85);
            });
            
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(entity =>
            {
                entity.Property(e => e.UserId).HasMaxLength(85);
                entity.Property(e => e.RoleId).HasMaxLength(85);
            });
            
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(entity =>
            {
                entity.Property(e => e.UserId).HasMaxLength(85);
                entity.Property(e => e.LoginProvider).HasMaxLength(85);
                entity.Property(e => e.Name).HasMaxLength(85);
            });
            
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(entity =>
            {
                entity.Property(e => e.UserId).HasMaxLength(85);
            });
            
            builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(entity =>
            {
                entity.Property(e => e.RoleId).HasMaxLength(85);
            });

            base.OnModelCreating(builder);

            // Explicitly tell EF Core about these types
            builder.Entity<Announcement>();
            builder.Entity<AnnouncementRead>();
            builder.Entity<ActivityLog>();
            builder.Entity<Facility>();
            builder.Entity<Event>();
            builder.Entity<Notification>();
            builder.Entity<Document>();
            builder.Entity<FacilityReservation>();
            builder.Entity<EventAttendee>();

            // Configure Announcement entity
            builder.Entity<Announcement>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Content).IsRequired();
                entity.Property(e => e.CreatedById).IsRequired();

                entity.HasOne(a => a.CreatedBy)
                    .WithMany()
                    .HasForeignKey(a => a.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure AnnouncementRead entity
            builder.Entity<AnnouncementRead>(entity =>
            {
                entity.HasKey(ar => new { ar.AnnouncementId, ar.UserId });

                entity.HasOne(ar => ar.Announcement)
                    .WithMany(a => a.ReadBy)
                    .HasForeignKey(ar => ar.AnnouncementId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ar => ar.User)
                    .WithMany()
                    .HasForeignKey(ar => ar.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ActivityLog entity
            builder.Entity<ActivityLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Module).IsRequired();
                entity.Property(e => e.Action).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.StatusColor).IsRequired();

                entity.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(a => a.Module);
                entity.HasIndex(a => a.CreatedAt);
            });

            // Configure Facility entity
            builder.Entity<Facility>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.Location).IsRequired();
                entity.Property(e => e.Capacity).HasDefaultValue(50);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.OpeningTime).HasDefaultValue(new TimeSpan(8, 0, 0)); // 8:00 AM
                entity.Property(e => e.ClosingTime).HasDefaultValue(new TimeSpan(22, 0, 0)); // 10:00 PM
                
                // Configure relationships
                entity.HasMany(f => f.Reservations)
                    .WithOne(r => r.Facility)
                    .HasForeignKey(r => r.FacilityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Event entity
            builder.Entity<Event>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Description).IsRequired();
                entity.Property(e => e.StartDate).IsRequired();
                entity.Property(e => e.EndDate).IsRequired();
                entity.Property(e => e.CreatedById).IsRequired();

                entity.HasOne(e => e.CreatedBy)
                    .WithMany()
                    .HasForeignKey(e => e.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Notification entity
            builder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.Message).IsRequired();
                entity.Property(e => e.UserId).IsRequired();

                entity.HasOne(n => n.User)
                    .WithMany()
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Document entity
            builder.Entity<Document>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired();
                entity.Property(e => e.FilePath).IsRequired();
                entity.Property(e => e.UploadedById).IsRequired();

                entity.HasOne(d => d.UploadedBy)
                    .WithMany()
                    .HasForeignKey(d => d.UploadedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure FacilityReservation entity
            builder.Entity<FacilityReservation>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.StartTime).IsRequired();
                entity.Property(e => e.EndTime).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.FacilityId).IsRequired();

                entity.HasOne(fr => fr.User)
                    .WithMany()
                    .HasForeignKey(fr => fr.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(fr => fr.Facility)
                    .WithMany()
                    .HasForeignKey(fr => fr.FacilityId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure EventAttendee entity
            builder.Entity<EventAttendee>(entity =>
            {
                entity.HasKey(ea => new { ea.EventId, ea.UserId });

                entity.HasOne(ea => ea.Event)
                    .WithMany()
                    .HasForeignKey(ea => ea.EventId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ea => ea.User)
                    .WithMany()
                    .HasForeignKey(ea => ea.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
} 
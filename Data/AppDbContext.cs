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

        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<AnnouncementRead> AnnouncementReads => Set<AnnouncementRead>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Explicitly tell EF Core about these types
            builder.Entity<Announcement>();
            builder.Entity<AnnouncementRead>();

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
        }
    }
} 
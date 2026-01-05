using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shikayat.Domain.Entities;

namespace Shikayat.Infrastructure.Data
{
    // We inherit from IdentityDbContext to get all the User/Role tables automatically
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Define our Tables
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Suggestion> Suggestions { get; set; }
        public DbSet<ComplaintLog> ComplaintLogs { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Location> Locations { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Rename Tables
            builder.Entity<Category>().ToTable("Categories");
            builder.Entity<Complaint>().ToTable("Complaints");

            // 2. Configure Location Hierarchy
            builder.Entity<Location>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.Children)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 3. Configure Category Hierarchy
            builder.Entity<Category>()
                .HasOne(x => x.Parent)
                .WithMany(x => x.SubCategories)
                .HasForeignKey(x => x.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Configure Complaint Relationships

            // Citizen Link (Keep Restrict)
            builder.Entity<Complaint>()
                .HasOne(c => c.Citizen)
                .WithMany()
                .HasForeignKey(c => c.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            // SubCategory Link (Keep Restrict)
            builder.Entity<Complaint>()
                .HasOne(c => c.SubCategory)
                .WithMany()
                .HasForeignKey(c => c.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- THE FIX: DISABLE CASCADE FOR LOCATIONS ---
            builder.Entity<Complaint>()
                .HasOne(c => c.Province)
                .WithMany()
                .HasForeignKey(c => c.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict); // <--- This stops the cycle

            builder.Entity<Complaint>()
                .HasOne(c => c.District)
                .WithMany()
                .HasForeignKey(c => c.DistrictId)
                .OnDelete(DeleteBehavior.Restrict); // <--- This stops the cycle

            builder.Entity<Complaint>()
                .HasOne(c => c.Tehsil)
                .WithMany()
                .HasForeignKey(c => c.TehsilId)
                .OnDelete(DeleteBehavior.Restrict); // <--- This stops the cycle

            // 5. Configure Suggestion Relationships (similar to Complaint)
            builder.Entity<Suggestion>()
                .HasOne(s => s.Citizen)
                .WithMany()
                .HasForeignKey(s => s.CitizenId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Suggestion>()
                .HasOne(s => s.SubCategory)
                .WithMany()
                .HasForeignKey(s => s.SubCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Suggestion>()
                .HasOne(s => s.Province)
                .WithMany()
                .HasForeignKey(s => s.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Suggestion>()
                .HasOne(s => s.District)
                .WithMany()
                .HasForeignKey(s => s.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Suggestion>()
                .HasOne(s => s.Tehsil)
                .WithMany()
                .HasForeignKey(s => s.TehsilId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Suggestion>().ToTable("Suggestions");
        }
    }
}
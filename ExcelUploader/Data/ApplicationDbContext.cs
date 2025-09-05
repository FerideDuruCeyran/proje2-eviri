using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ExcelUploader.Models;

namespace ExcelUploader.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<DynamicTable> DynamicTables { get; set; }
        public DbSet<LoginLog> LoginLogs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure DynamicTable
            builder.Entity<DynamicTable>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.TableName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.TableName).IsUnique();
                entity.HasIndex(e => e.UploadDate);
            });

            // Configure LoginLog
            builder.Entity<LoginLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.Message).HasMaxLength(500);
                entity.Property(e => e.IpAddress).HasMaxLength(45);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.LoginTime);
            });

            // Seed default admin user
            SeedDefaultUser(builder);
        }

        private void SeedDefaultUser(ModelBuilder builder)
        {
            var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<ApplicationUser>();
            var adminUser = new ApplicationUser
            {
                Id = "1",
                UserName = "admin@exceluploader.com",
                Email = "admin@exceluploader.com",
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true,
                NormalizedEmail = "ADMIN@EXCELUPLOADER.COM",
                NormalizedUserName = "ADMIN@EXCELUPLOADER.COM",
                SecurityStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            adminUser.PasswordHash = hasher.HashPassword(adminUser, "admin");

            builder.Entity<ApplicationUser>().HasData(adminUser);
        }
    }
}

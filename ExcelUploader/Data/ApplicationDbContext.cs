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

        public DbSet<ExcelData> ExcelData { get; set; }
        public DbSet<DynamicTable> DynamicTables { get; set; }
        public DbSet<TableColumn> TableColumns { get; set; }
        public DbSet<TableData> TableData { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ExcelData table
            builder.Entity<ExcelData>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                
                // Configure string properties to handle large text
                entity.Property(e => e.Aciklama).HasMaxLength(2000);
                entity.Property(e => e.BasvuruAciklama).HasMaxLength(2000);
                entity.Property(e => e.UniversitedeToplamCalismaSuresi).HasMaxLength(500);
                
                // Configure decimal properties
                entity.Property(e => e.Odenecek).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Odendiginde).HasColumnType("decimal(18,2)");
                entity.Property(e => e.OdemeOrani).HasColumnType("decimal(5,2)");
                entity.Property(e => e.AkademikOrtalama).HasColumnType("decimal(5,2)");
                entity.Property(e => e.HibeOdemeOrani).HasColumnType("decimal(5,2)");
                entity.Property(e => e.HibeOdeneceklerToplami).HasColumnType("decimal(18,2)");
                entity.Property(e => e.HibeOdenenlerToplami).HasColumnType("decimal(18,2)");
                entity.Property(e => e.SinavPuani).HasColumnType("decimal(5,2)");
                
                // Configure date properties
                entity.Property(e => e.OdemeTarihi).HasColumnType("date");
                entity.Property(e => e.DogumTarihi).HasColumnType("date");
                entity.Property(e => e.BasvuruTarihi).HasColumnType("date");
                entity.Property(e => e.HareketlilikBaslangicTarihi).HasColumnType("date");
                entity.Property(e => e.HareketlilikBitisTarihi).HasColumnType("date");
                entity.Property(e => e.SinavTarihi).HasColumnType("date");
                
                // Configure indexes for better performance
                entity.HasIndex(e => e.FileName);
                entity.HasIndex(e => e.UploadDate);
                entity.HasIndex(e => e.IsProcessed);
                entity.HasIndex(e => e.BasvuruYili);
                entity.HasIndex(e => e.HareketlilikTipi);
                entity.HasIndex(e => e.OdemeTipi);
                entity.HasIndex(e => e.Ad);
                entity.HasIndex(e => e.Soyad);
                entity.HasIndex(e => e.TCKimlikNo);
                                    entity.HasIndex(e => e.OgrenciNo);
                });

                // Configure DynamicTable
                builder.Entity<DynamicTable>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();
                    entity.Property(e => e.TableName).HasMaxLength(128).IsRequired();
                    entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
                    entity.Property(e => e.Description).HasMaxLength(1000);
                    entity.HasIndex(e => e.TableName).IsUnique();
                    entity.HasIndex(e => e.FileName);
                    entity.HasIndex(e => e.UploadDate);
                });

                // Configure TableColumn
                builder.Entity<TableColumn>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();
                    entity.Property(e => e.ColumnName).HasMaxLength(128).IsRequired();
                    entity.Property(e => e.DisplayName).HasMaxLength(255).IsRequired();
                    entity.Property(e => e.DataType).HasMaxLength(50).IsRequired();
                    entity.HasIndex(e => new { e.DynamicTableId, e.ColumnOrder });
                    entity.HasOne(e => e.DynamicTable)
                          .WithMany(e => e.Columns)
                          .HasForeignKey(e => e.DynamicTableId)
                          .OnDelete(DeleteBehavior.Cascade);
                });

                // Configure TableData
                builder.Entity<TableData>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();
                    entity.Property(e => e.Data).HasMaxLength(4000);
                    entity.HasIndex(e => new { e.DynamicTableId, e.RowNumber });
                    entity.HasOne(e => e.DynamicTable)
                          .WithMany(e => e.Data)
                          .HasForeignKey(e => e.DynamicTableId)
                          .OnDelete(DeleteBehavior.Cascade);
                    entity.HasOne(e => e.Column)
                          .WithMany()
                          .HasForeignKey(e => e.ColumnId)
                          .OnDelete(DeleteBehavior.NoAction);
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

            adminUser.PasswordHash = hasher.HashPassword(adminUser, "Admin123!");

            builder.Entity<ApplicationUser>().HasData(adminUser);
        }
    }
}

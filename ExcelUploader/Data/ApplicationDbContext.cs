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
        public DbSet<UserLoginLog> UserLoginLogs { get; set; }
        public DbSet<LoginLog> LoginLogs { get; set; }
        public DbSet<DatabaseConnection> DatabaseConnections { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.ConfigureWarnings(warnings => 
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }

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
                
                // Configure address and location fields to 250 characters
                entity.Property(e => e.AdresIl).HasMaxLength(250);
                entity.Property(e => e.AdresUlke).HasMaxLength(250);
                entity.Property(e => e.DogumYeri).HasMaxLength(250);
                
                // Configure bank-related fields to 250 characters
                entity.Property(e => e.BankaHesapSahibi).HasMaxLength(250);
                entity.Property(e => e.BankaAdi).HasMaxLength(250);
                entity.Property(e => e.BankaSubeKodu).HasMaxLength(250);
                entity.Property(e => e.BankaSubeAdi).HasMaxLength(250);
                entity.Property(e => e.BankaHesapNumarasi).HasMaxLength(250);
                entity.Property(e => e.BankaIBANNo).HasMaxLength(250);
                
                // Configure university and academic fields to 250 characters
                entity.Property(e => e.FakulteAdi).HasMaxLength(250);
                entity.Property(e => e.BirimAdi).HasMaxLength(250);
                entity.Property(e => e.DiplomaDerecesi).HasMaxLength(250);
                entity.Property(e => e.UniversiteKoordinatoru).HasMaxLength(250);
                entity.Property(e => e.UniversiteKoordinatoruEmail).HasMaxLength(250);
                entity.Property(e => e.UniversiteUluslararasiKodu).HasMaxLength(250);
                entity.Property(e => e.UzmanlikAlani).HasMaxLength(250);
                entity.Property(e => e.BasvuruSayfasi).HasMaxLength(250);
                
                // Configure other fields that might contain longer text to 250 characters
                entity.Property(e => e.BasvuruTipi).HasMaxLength(250);
                entity.Property(e => e.Taksit).HasMaxLength(250);
                entity.Property(e => e.KullaniciAdi).HasMaxLength(250);
                entity.Property(e => e.PasaportNo).HasMaxLength(250);
                entity.Property(e => e.Cinsiyet).HasMaxLength(250);
                entity.Property(e => e.Sinif).HasMaxLength(250);
                entity.Property(e => e.GaziSehitYakini).HasMaxLength(250);
                entity.Property(e => e.YurtBasvurusu).HasMaxLength(250);
                entity.Property(e => e.TercihSirasi).HasMaxLength(250);
                entity.Property(e => e.TercihDurumu).HasMaxLength(250);
                entity.Property(e => e.BasvuruDurumu).HasMaxLength(250);
                entity.Property(e => e.Burs).HasMaxLength(250);
                entity.Property(e => e.AkademikYil).HasMaxLength(250);
                entity.Property(e => e.AkademikDonem).HasMaxLength(250);
                entity.Property(e => e.DegisimProgramiTipi).HasMaxLength(250);
                entity.Property(e => e.KatilmakIstedigiYabanciDilSinavi).HasMaxLength(250);
                entity.Property(e => e.SistemDisiGecmisHareketlilik).HasMaxLength(250);
                entity.Property(e => e.SistemIciGecmisHareketlilikBilgisi).HasMaxLength(250);
                entity.Property(e => e.Tercihler).HasMaxLength(250);
                entity.Property(e => e.HibeSozlesmeTipi).HasMaxLength(250);
                entity.Property(e => e.HibeButceYili).HasMaxLength(250);
                entity.Property(e => e.SinavTipi).HasMaxLength(250);
                entity.Property(e => e.SinavDili).HasMaxLength(250);
                entity.Property(e => e.Unvan).HasMaxLength(250);
                
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

                // Configure UserLoginLog
                builder.Entity<UserLoginLog>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();
                    entity.Property(e => e.Timestamp).HasColumnType("datetime2");
                    entity.HasIndex(e => e.UserId);
                    entity.HasIndex(e => e.Timestamp);
                    entity.HasIndex(e => e.Action);
                    entity.HasIndex(e => e.IsSuccessful);
                    entity.HasOne(e => e.User)
                          .WithMany()
                          .HasForeignKey(e => e.UserId)
                          .OnDelete(DeleteBehavior.Cascade);
                });

                // Configure DatabaseConnection
                builder.Entity<DatabaseConnection>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();
                    entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                    entity.Property(e => e.ServerName).HasMaxLength(255).IsRequired();
                    entity.Property(e => e.DatabaseName).HasMaxLength(128).IsRequired();
                    entity.Property(e => e.Username).HasMaxLength(100).IsRequired();
                    entity.Property(e => e.Password).HasMaxLength(255).IsRequired();
                    entity.Property(e => e.Description).HasMaxLength(1000);
                    entity.HasIndex(e => e.Name).IsUnique();
                    entity.HasIndex(e => e.IsActive);
                });

                // Configure LoginLog
                builder.Entity<LoginLog>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Id).ValueGeneratedOnAdd();
                    entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
                    entity.Property(e => e.UserName).HasMaxLength(256).IsRequired();
                    entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
                    entity.Property(e => e.IpAddress).HasMaxLength(45);
                    entity.Property(e => e.UserAgent).HasMaxLength(500);
                    entity.Property(e => e.FailureReason).HasMaxLength(500);
                    entity.Property(e => e.SessionId).HasMaxLength(100);
                    entity.Property(e => e.LoginTime).HasColumnType("datetime2");
                    entity.Property(e => e.LogoutTime).HasColumnType("datetime2");
                    entity.HasIndex(e => e.UserId);
                    entity.HasIndex(e => e.LoginTime);
                    entity.HasIndex(e => e.IsSuccess);
                    entity.HasIndex(e => e.SessionId);
                    entity.HasOne(e => e.User)
                          .WithMany()
                          .HasForeignKey(e => e.UserId)
                          .OnDelete(DeleteBehavior.Cascade);
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

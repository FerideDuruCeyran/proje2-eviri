using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelUploader.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ProfilePicture = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatabaseConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServerName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Port = table.Column<int>(type: "int", nullable: false),
                    DatabaseName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Password = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTestDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastTestResult = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatabaseConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DynamicTables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TableName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    ColumnCount = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DynamicTables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExcelData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BasvuruYili = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    HareketlilikTipi = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    BasvuruTipi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ad = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Soyad = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    OdemeTipi = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Taksit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Odenecek = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Odendiginde = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OdemeTarihi = table.Column<DateTime>(type: "date", nullable: true),
                    Aciklama = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OdemeOrani = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    KullaniciAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TCKimlikNo = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PasaportNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DogumTarihi = table.Column<DateTime>(type: "date", nullable: true),
                    DogumYeri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Cinsiyet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdresIl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdresUlke = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankaHesapSahibi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankaAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankaSubeKodu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankaSubeAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankaHesapNumarasi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BankaIBANNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OgrenciNo = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    FakulteAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BirimAdi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DiplomaDerecesi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sinif = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasvuruAciklama = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    GaziSehitYakini = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YurtBasvurusu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AkademikOrtalama = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    TercihSirasi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TercihDurumu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasvuruDurumu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Burs = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AkademikYil = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AkademikDonem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BasvuruTarihi = table.Column<DateTime>(type: "date", nullable: true),
                    DegisimProgramiTipi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KatilmakIstedigiYabanciDilSinavi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SistemDisiGecmisHareketlilik = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SistemIciGecmisHareketlilikBilgisi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tercihler = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HibeSozlesmeTipi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HibeButceYili = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HibeOdemeOrani = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    HibeOdeneceklerToplami = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HibeOdenenlerToplami = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    HareketlilikBaslangicTarihi = table.Column<DateTime>(type: "date", nullable: true),
                    HareketlilikBitisTarihi = table.Column<DateTime>(type: "date", nullable: true),
                    PlanlananToplamHibeliGunSayisi = table.Column<int>(type: "int", nullable: true),
                    GerceklesenToplamHibeliGun = table.Column<int>(type: "int", nullable: true),
                    UniversiteKoordinatoru = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UniversiteKoordinatoruEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UniversiteUluslararasiKodu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SinavTipi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SinavPuani = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    SinavTarihi = table.Column<DateTime>(type: "date", nullable: true),
                    SinavDili = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unvan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UzmanlikAlani = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UniversitedeToplamCalismaSuresi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BasvuruSayfasi = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcelData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLoginLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdditionalInfo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLoginLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserLoginLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TableColumns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ColumnName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ColumnOrder = table.Column<int>(type: "int", nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: true),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsUnique = table.Column<bool>(type: "bit", nullable: false),
                    DynamicTableId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableColumns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableColumns_DynamicTables_DynamicTableId",
                        column: x => x.DynamicTableId,
                        principalTable: "DynamicTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TableData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RowNumber = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ColumnId = table.Column<int>(type: "int", nullable: false),
                    DynamicTableId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TableData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TableData_DynamicTables_DynamicTableId",
                        column: x => x.DynamicTableId,
                        principalTable: "DynamicTables",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TableData_TableColumns_ColumnId",
                        column: x => x.ColumnId,
                        principalTable: "TableColumns",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "ConcurrencyStamp", "CreatedAt", "Email", "EmailConfirmed", "FirstName", "IsActive", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "ProfilePicture", "SecurityStamp", "TwoFactorEnabled", "UserName" },
                values: new object[] { "1", 0, "acec07c4-e38d-4f58-8de7-660f8dc6765a", new DateTime(2025, 8, 29, 7, 45, 48, 874, DateTimeKind.Utc).AddTicks(3183), "admin@exceluploader.com", true, "Admin", true, "User", false, null, "ADMIN@EXCELUPLOADER.COM", "ADMIN@EXCELUPLOADER.COM", "AQAAAAIAAYagAAAAEFrv79kNe61x1x1N+nBKCUU/3/p44ua0l7Qwszrk9ctxNx6G/galWv7HMEQhX1MIjg==", null, false, null, "9d056241-c713-4d03-a583-83eda4c8dde3", false, "admin@exceluploader.com" });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_IsActive",
                table: "DatabaseConnections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_DatabaseConnections_Name",
                table: "DatabaseConnections",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DynamicTables_FileName",
                table: "DynamicTables",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_DynamicTables_TableName",
                table: "DynamicTables",
                column: "TableName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DynamicTables_UploadDate",
                table: "DynamicTables",
                column: "UploadDate");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_Ad",
                table: "ExcelData",
                column: "Ad");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_BasvuruYili",
                table: "ExcelData",
                column: "BasvuruYili");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_FileName",
                table: "ExcelData",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_HareketlilikTipi",
                table: "ExcelData",
                column: "HareketlilikTipi");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_IsProcessed",
                table: "ExcelData",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_OdemeTipi",
                table: "ExcelData",
                column: "OdemeTipi");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_OgrenciNo",
                table: "ExcelData",
                column: "OgrenciNo");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_Soyad",
                table: "ExcelData",
                column: "Soyad");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_TCKimlikNo",
                table: "ExcelData",
                column: "TCKimlikNo");

            migrationBuilder.CreateIndex(
                name: "IX_ExcelData_UploadDate",
                table: "ExcelData",
                column: "UploadDate");

            migrationBuilder.CreateIndex(
                name: "IX_TableColumns_DynamicTableId_ColumnOrder",
                table: "TableColumns",
                columns: new[] { "DynamicTableId", "ColumnOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_TableData_ColumnId",
                table: "TableData",
                column: "ColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_TableData_DynamicTableId_RowNumber",
                table: "TableData",
                columns: new[] { "DynamicTableId", "RowNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginLogs_Action",
                table: "UserLoginLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginLogs_IsSuccessful",
                table: "UserLoginLogs",
                column: "IsSuccessful");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginLogs_Timestamp",
                table: "UserLoginLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_UserLoginLogs_UserId",
                table: "UserLoginLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "DatabaseConnections");

            migrationBuilder.DropTable(
                name: "ExcelData");

            migrationBuilder.DropTable(
                name: "TableData");

            migrationBuilder.DropTable(
                name: "UserLoginLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "TableColumns");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "DynamicTables");
        }
    }
}

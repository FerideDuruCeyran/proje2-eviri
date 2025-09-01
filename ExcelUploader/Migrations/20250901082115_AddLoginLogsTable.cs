using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelUploader.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LoginLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LogoutTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoginLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "95fc9748-563c-4682-9ed2-89db3d0fc2ce", new DateTime(2025, 9, 1, 8, 21, 14, 351, DateTimeKind.Utc).AddTicks(5654), "AQAAAAIAAYagAAAAEGV7jJ6NYI52nLFHfFNiYnuGgtTBZiuWq8Q/SOXusZX9oIYjps7ElPYBdW2nKidAig==", "6ee3251c-4426-46b3-a838-76a4fb5933cd" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_IsSuccess",
                table: "LoginLogs",
                column: "IsSuccess");

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_LoginTime",
                table: "LoginLogs",
                column: "LoginTime");

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_SessionId",
                table: "LoginLogs",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginLogs_UserId",
                table: "LoginLogs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginLogs");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "acec07c4-e38d-4f58-8de7-660f8dc6765a", new DateTime(2025, 8, 29, 7, 45, 48, 874, DateTimeKind.Utc).AddTicks(3183), "AQAAAAIAAYagAAAAEFrv79kNe61x1x1N+nBKCUU/3/p44ua0l7Qwszrk9ctxNx6G/galWv7HMEQhX1MIjg==", "9d056241-c713-4d03-a583-83eda4c8dde3" });
        }
    }
}

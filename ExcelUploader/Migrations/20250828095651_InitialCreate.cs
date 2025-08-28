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
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2b3657b1-fff3-402e-a918-44c0dcd1a272", new DateTime(2025, 8, 28, 9, 56, 50, 563, DateTimeKind.Utc).AddTicks(9997), "AQAAAAIAAYagAAAAENDXVHJo8a6gMcwfNZQrwXtXioHI7xCb/V7ts3CQTK3E3JsDHqnvAU1KJAmV12zhug==", "6d98fa86-cba4-4b4d-bc4c-1d66bc063e20" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "1",
                columns: new[] { "ConcurrencyStamp", "CreatedAt", "PasswordHash", "SecurityStamp" },
                values: new object[] { "d0d7620b-2cb0-415b-b891-9721b2126832", new DateTime(2025, 8, 28, 9, 56, 23, 343, DateTimeKind.Utc).AddTicks(9154), "AQAAAAIAAYagAAAAEDgCayLxQBPVXqDZNQ3cfOKAekP1Ed/KgYPAruYPo2Oe+wbwyAlEhNGyFTMCUkNZ5Q==", "9e818ca4-cd2b-4020-82b1-0a50d1e7e9c1" });
        }
    }
}

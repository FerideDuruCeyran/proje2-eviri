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
            // Create AspNetRoles table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoles')
                BEGIN
                    CREATE TABLE [AspNetRoles] (
                        [Id] nvarchar(450) NOT NULL,
                        [Name] nvarchar(256) NULL,
                        [NormalizedName] nvarchar(256) NULL,
                        [ConcurrencyStamp] nvarchar(max) NULL,
                        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
                    );
                END
            ");

            // Create AspNetUsers table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUsers')
                BEGIN
                    CREATE TABLE [AspNetUsers] (
                        [Id] nvarchar(450) NOT NULL,
                        [FirstName] nvarchar(max) NOT NULL,
                        [LastName] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [IsActive] bit NOT NULL,
                        [ProfilePicture] nvarchar(max) NULL,
                        [UserName] nvarchar(256) NULL,
                        [NormalizedUserName] nvarchar(256) NULL,
                        [Email] nvarchar(256) NULL,
                        [NormalizedEmail] nvarchar(256) NULL,
                        [EmailConfirmed] bit NOT NULL,
                        [PasswordHash] nvarchar(max) NULL,
                        [SecurityStamp] nvarchar(max) NULL,
                        [ConcurrencyStamp] nvarchar(max) NULL,
                        [PhoneNumber] nvarchar(max) NULL,
                        [PhoneNumberConfirmed] bit NOT NULL,
                        [TwoFactorEnabled] bit NOT NULL,
                        [LockoutEnd] datetimeoffset NULL,
                        [LockoutEnabled] bit NOT NULL,
                        [AccessFailedCount] int NOT NULL,
                        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
                    );
                END
            ");

            // Create DynamicTables table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DynamicTables')
                BEGIN
                    CREATE TABLE [DynamicTables] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [TableName] nvarchar(128) NOT NULL,
                        [FileName] nvarchar(255) NOT NULL,
                        [UploadDate] datetime2 NOT NULL,
                        [UploadedBy] nvarchar(max) NOT NULL,
                        [RowCount] int NOT NULL,
                        [ColumnCount] int NOT NULL,
                        [Description] nvarchar(1000) NULL,
                        [IsProcessed] bit NOT NULL,
                        [ProcessedDate] datetime2 NULL,
                        CONSTRAINT [PK_DynamicTables] PRIMARY KEY ([Id])
                    );
                END
            ");

            // Create TableColumns table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TableColumns')
                BEGIN
                    CREATE TABLE [TableColumns] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [ColumnName] nvarchar(128) NOT NULL,
                        [DisplayName] nvarchar(255) NOT NULL,
                        [DataType] nvarchar(50) NOT NULL,
                        [ColumnOrder] int NOT NULL,
                        [MaxLength] int NULL,
                        [IsRequired] bit NOT NULL,
                        [IsUnique] bit NOT NULL,
                        [DynamicTableId] int NOT NULL,
                        CONSTRAINT [PK_TableColumns] PRIMARY KEY ([Id])
                    );
                END
            ");

            // Create TableData table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TableData')
                BEGIN
                    CREATE TABLE [TableData] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [RowNumber] int NOT NULL,
                        [Data] nvarchar(4000) NOT NULL,
                        [ColumnId] int NOT NULL,
                        [DynamicTableId] int NOT NULL,
                        CONSTRAINT [PK_TableData] PRIMARY KEY ([Id])
                    );
                END
            ");

            // Create AspNetRoleClaims table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetRoleClaims')
                BEGIN
                    CREATE TABLE [AspNetRoleClaims] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [RoleId] nvarchar(450) NOT NULL,
                        [ClaimType] nvarchar(max) NULL,
                        [ClaimValue] nvarchar(max) NULL,
                        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id])
                    );
                END
            ");

            // Create AspNetUserClaims table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserClaims')
                BEGIN
                    CREATE TABLE [AspNetUserClaims] (
                        [Id] int NOT NULL IDENTITY(1,1),
                        [UserId] nvarchar(450) NOT NULL,
                        [ClaimType] nvarchar(max) NULL,
                        [ClaimValue] nvarchar(max) NULL,
                        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id])
                    );
                END
            ");

            // Create AspNetUserLogins table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserLogins')
                BEGIN
                    CREATE TABLE [AspNetUserLogins] (
                        [LoginProvider] nvarchar(450) NOT NULL,
                        [ProviderKey] nvarchar(450) NOT NULL,
                        [ProviderDisplayName] nvarchar(max) NULL,
                        [UserId] nvarchar(450) NOT NULL,
                        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey])
                    );
                END
            ");

            // Create AspNetUserRoles table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserRoles')
                BEGIN
                    CREATE TABLE [AspNetUserRoles] (
                        [UserId] nvarchar(450) NOT NULL,
                        [RoleId] nvarchar(450) NOT NULL,
                        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId])
                    );
                END
            ");

            // Create AspNetUserTokens table if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AspNetUserTokens')
                BEGIN
                    CREATE TABLE [AspNetUserTokens] (
                        [UserId] nvarchar(450) NOT NULL,
                        [LoginProvider] nvarchar(450) NOT NULL,
                        [Name] nvarchar(450) NOT NULL,
                        [Value] nvarchar(max) NULL,
                        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name])
                    );
                END
            ");

            // Insert default admin user if it doesn't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM [AspNetUsers] WHERE [Email] = 'admin@exceluploader.com')
                BEGIN
                    INSERT INTO [AspNetUsers] ([Id], [FirstName], [LastName], [CreatedAt], [IsActive], [UserName], [NormalizedUserName], [Email], [NormalizedEmail], [EmailConfirmed], [PasswordHash], [SecurityStamp], [ConcurrencyStamp], [PhoneNumber], [PhoneNumberConfirmed], [TwoFactorEnabled], [LockoutEnd], [LockoutEnabled], [AccessFailedCount])
                    VALUES ('1', 'Admin', 'User', GETUTCDATE(), 1, 'admin@exceluploader.com', 'ADMIN@EXCELUPLOADER.COM', 'admin@exceluploader.com', 'ADMIN@EXCELUPLOADER.COM', 1, 'AQAAAAIAAYagAAAAEKhCl5bs/HR+Hl4fN4MWddjjZgqdbKcMka+MaTNW/EEdnoybitcVAoz0NAzjcESGYw==', NEWID(), NEWID(), NULL, 0, 0, NULL, 0, 0);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop tables in reverse order
            migrationBuilder.Sql("DROP TABLE IF EXISTS [AspNetUserTokens]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [AspNetUserRoles]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [AspNetUserLogins]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [AspNetUserClaims]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [AspNetRoleClaims]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [TableData]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [TableColumns]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [DynamicTables]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [AspNetUsers]");
            migrationBuilder.Sql("DROP TABLE IF EXISTS [AspNetRoles]");
        }
    }
}

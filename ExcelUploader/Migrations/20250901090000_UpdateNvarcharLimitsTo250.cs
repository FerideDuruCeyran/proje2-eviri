using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ExcelUploader.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNvarcharLimitsTo250 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update address and location fields to 250 characters
            migrationBuilder.AlterColumn<string>(
                name: "AdresIl",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdresUlke",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DogumYeri",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            // Update bank-related fields to 250 characters
            migrationBuilder.AlterColumn<string>(
                name: "BankaHesapSahibi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaAdi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaSubeKodu",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaSubeAdi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaHesapNumarasi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaIBANNo",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            // Update university and academic fields to 250 characters
            migrationBuilder.AlterColumn<string>(
                name: "FakulteAdi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BirimAdi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiplomaDerecesi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UniversiteKoordinatoru",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UniversiteKoordinatoruEmail",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UniversiteUluslararasiKodu",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UzmanlikAlani",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BasvuruSayfasi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            // Update other fields that might contain longer text to 250 characters
            migrationBuilder.AlterColumn<string>(
                name: "BasvuruTipi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Taksit",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KullaniciAdi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasaportNo",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Cinsiyet",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sinif",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GaziSehitYakini",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "YurtBasvurusu",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TercihSirasi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TercihDurumu",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BasvuruDurumu",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Burs",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AkademikYil",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AkademikDonem",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DegisimProgramiTipi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KatilmakIstedigiYabanciDilSinavi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SistemDisiGecmisHareketlilik",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SistemIciGecmisHareketlilikBilgisi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Tercihler",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HibeSozlesmeTipi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HibeButceYili",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SinavTipi",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SinavDili",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Unvan",
                table: "ExcelData",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert address and location fields back to max
            migrationBuilder.AlterColumn<string>(
                name: "AdresIl",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AdresUlke",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DogumYeri",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            // Revert bank-related fields back to max
            migrationBuilder.AlterColumn<string>(
                name: "BankaHesapSahibi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaAdi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaSubeKodu",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaSubeAdi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaHesapNumarasi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BankaIBANNo",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            // Revert university and academic fields back to max
            migrationBuilder.AlterColumn<string>(
                name: "FakulteAdi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BirimAdi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiplomaDerecesi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UniversiteKoordinatoru",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UniversiteKoordinatoruEmail",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UniversiteUluslararasiKodu",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UzmanlikAlani",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BasvuruSayfasi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            // Revert other fields back to max
            migrationBuilder.AlterColumn<string>(
                name: "BasvuruTipi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Taksit",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KullaniciAdi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PasaportNo",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Cinsiyet",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Sinif",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "GaziSehitYakini",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "YurtBasvurusu",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TercihSirasi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TercihDurumu",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BasvuruDurumu",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Burs",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AkademikYil",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AkademikDonem",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DegisimProgramiTipi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "KatilmakIstedigiYabanciDilSinavi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SistemDisiGecmisHareketlilik",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SistemIciGecmisHareketlilikBilgisi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Tercihler",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HibeSozlesmeTipi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "HibeButceYili",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SinavTipi",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SinavDili",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Unvan",
                table: "ExcelData",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}

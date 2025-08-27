using System.ComponentModel.DataAnnotations;

namespace ExcelUploader.Models
{
    public class ExcelData
    {
        public int Id { get; set; }
        
        [Display(Name = "Başvuru Yılı")]
        public string? BasvuruYili { get; set; }
        
        [Display(Name = "Hareketlilik Tipi")]
        public string? HareketlilikTipi { get; set; }
        
        [Display(Name = "Başvuru Tipi")]
        public string? BasvuruTipi { get; set; }
        
        [Display(Name = "Ad")]
        public string? Ad { get; set; }
        
        [Display(Name = "Soyad")]
        public string? Soyad { get; set; }
        
        [Display(Name = "Ödeme Tipi")]
        public string? OdemeTipi { get; set; }
        
        [Display(Name = "Taksit")]
        public string? Taksit { get; set; }
        
        [Display(Name = "Ödenecek")]
        public decimal? Odenecek { get; set; }
        
        [Display(Name = "Ödenen")]
        public decimal? Odendiginde { get; set; }
        
        [Display(Name = "Ödeme Tarihi")]
        public DateTime? OdemeTarihi { get; set; }
        
        [Display(Name = "Açıklama")]
        public string? Aciklama { get; set; }
        
        [Display(Name = "Ödeme Oranı")]
        public decimal? OdemeOrani { get; set; }
        
        [Display(Name = "Kullanıcı Adı")]
        public string? KullaniciAdi { get; set; }
        
        [Display(Name = "TC Kimlik No")]
        public string? TCKimlikNo { get; set; }
        
        [Display(Name = "Pasaport No")]
        public string? PasaportNo { get; set; }
        
        [Display(Name = "Doğum Tarihi")]
        public DateTime? DogumTarihi { get; set; }
        
        [Display(Name = "Doğum Yeri")]
        public string? DogumYeri { get; set; }
        
        [Display(Name = "Cinsiyet")]
        public string? Cinsiyet { get; set; }
        
        [Display(Name = "Adres İl")]
        public string? AdresIl { get; set; }
        
        [Display(Name = "Adres Ülke")]
        public string? AdresUlke { get; set; }
        
        [Display(Name = "Banka Hesap Sahibi")]
        public string? BankaHesapSahibi { get; set; }
        
        [Display(Name = "Banka Adı")]
        public string? BankaAdi { get; set; }
        
        [Display(Name = "Banka Şube Kodu")]
        public string? BankaSubeKodu { get; set; }
        
        [Display(Name = "Banka Şube Adı")]
        public string? BankaSubeAdi { get; set; }
        
        [Display(Name = "Banka Hesap Numarası")]
        public string? BankaHesapNumarasi { get; set; }
        
        [Display(Name = "Banka IBAN No")]
        public string? BankaIBANNo { get; set; }
        
        [Display(Name = "Öğrenci No")]
        public string? OgrenciNo { get; set; }
        
        [Display(Name = "Fakülte Adı")]
        public string? FakulteAdi { get; set; }
        
        [Display(Name = "Birim Adı")]
        public string? BirimAdi { get; set; }
        
        [Display(Name = "Diploma Derecesi")]
        public string? DiplomaDerecesi { get; set; }
        
        [Display(Name = "Sınıf")]
        public string? Sinif { get; set; }
        
        [Display(Name = "Başvuru Açıklama")]
        public string? BasvuruAciklama { get; set; }
        
        [Display(Name = "Gazi Şehit Yakını")]
        public string? GaziSehitYakini { get; set; }
        
        [Display(Name = "Yurt Başvurusu")]
        public string? YurtBasvurusu { get; set; }
        
        [Display(Name = "Akademik Ortalama")]
        public decimal? AkademikOrtalama { get; set; }
        
        [Display(Name = "Tercih Sırası")]
        public string? TercihSirasi { get; set; }
        
        [Display(Name = "Tercih Durumu")]
        public string? TercihDurumu { get; set; }
        
        [Display(Name = "Başvuru Durumu")]
        public string? BasvuruDurumu { get; set; }
        
        [Display(Name = "Burs")]
        public string? Burs { get; set; }
        
        [Display(Name = "Akademik Yıl")]
        public string? AkademikYil { get; set; }
        
        [Display(Name = "Akademik Dönem")]
        public string? AkademikDonem { get; set; }
        
        [Display(Name = "Başvuru Tarihi")]
        public DateTime? BasvuruTarihi { get; set; }
        
        [Display(Name = "Değişim Programı Tipi")]
        public string? DegisimProgramiTipi { get; set; }
        
        [Display(Name = "Katılmak İstediği Yabancı Dil Sınavı")]
        public string? KatilmakIstedigiYabanciDilSinavi { get; set; }
        
        [Display(Name = "Sistem Dışı Geçmiş Hareketlilik")]
        public string? SistemDisiGecmisHareketlilik { get; set; }
        
        [Display(Name = "Sistem İçi Geçmiş Hareketlilik Bilgisi")]
        public string? SistemIciGecmisHareketlilikBilgisi { get; set; }
        
        [Display(Name = "Tercihler")]
        public string? Tercihler { get; set; }
        
        [Display(Name = "Hibe Sözleşme Tipi")]
        public string? HibeSozlesmeTipi { get; set; }
        
        [Display(Name = "Hibe Bütçe Yılı")]
        public string? HibeButceYili { get; set; }
        
        [Display(Name = "Hibe Ödeme Oranı")]
        public decimal? HibeOdemeOrani { get; set; }
        
        [Display(Name = "Hibe Ödenecekler Toplamı")]
        public decimal? HibeOdeneceklerToplami { get; set; }
        
        [Display(Name = "Hibe Ödenenler Toplamı")]
        public decimal? HibeOdenenlerToplami { get; set; }
        
        [Display(Name = "Hareketlilik Başlangıç Tarihi")]
        public DateTime? HareketlilikBaslangicTarihi { get; set; }
        
        [Display(Name = "Hareketlilik Bitiş Tarihi")]
        public DateTime? HareketlilikBitisTarihi { get; set; }
        
        [Display(Name = "Planlanan Toplam Hibeli Gün Sayısı")]
        public int? PlanlananToplamHibeliGunSayisi { get; set; }
        
        [Display(Name = "Gerçekleşen Toplam Hibeli Gün")]
        public int? GerceklesenToplamHibeliGun { get; set; }
        
        [Display(Name = "Üniversite Koordinatörü")]
        public string? UniversiteKoordinatoru { get; set; }
        
        [Display(Name = "Üniversite Koordinatörü Email")]
        public string? UniversiteKoordinatoruEmail { get; set; }
        
        [Display(Name = "Üniversite Uluslararası Kodu")]
        public string? UniversiteUluslararasiKodu { get; set; }
        
        [Display(Name = "Sınav Tipi")]
        public string? SinavTipi { get; set; }
        
        [Display(Name = "Sınav Puanı")]
        public decimal? SinavPuani { get; set; }
        
        [Display(Name = "Sınav Tarihi")]
        public DateTime? SinavTarihi { get; set; }
        
        [Display(Name = "Sınav Dili")]
        public string? SinavDili { get; set; }
        
        [Display(Name = "Ünvan")]
        public string? Unvan { get; set; }
        
        [Display(Name = "Uzmanlık Alanı")]
        public string? UzmanlikAlani { get; set; }
        
        [Display(Name = "Üniversitede Toplam Çalışma Süresi")]
        public string? UniversitedeToplamCalismaSuresi { get; set; }
        
        [Display(Name = "Başvuru Sayfası")]
        public string? BasvuruSayfasi { get; set; }
        
        // Metadata fields
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        public string UploadedBy { get; set; } = string.Empty;
        public int RowNumber { get; set; }
        public bool IsProcessed { get; set; } = false;
        public DateTime? ProcessedDate { get; set; }
    }
}

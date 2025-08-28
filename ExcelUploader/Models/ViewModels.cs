using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ExcelUploader.Models
{
    public class UploadViewModel
    {
        [Required(ErrorMessage = "Lütfen bir Excel dosyası seçin")]
        [Display(Name = "Excel Dosyası")]
        public IFormFile? ExcelFile { get; set; }
        
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }
        
        public bool IsProcessing { get; set; }
        public string? ProcessingMessage { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int PendingRecords { get; set; }
        public decimal TotalGrantAmount { get; set; }
        public decimal TotalPaidAmount { get; set; }
        public List<ExcelData> RecentUploads { get; set; } = new();
        public List<ChartData> MonthlyData { get; set; } = new();
        public List<DynamicTable> DynamicTables { get; set; } = new();
    }

    public class ChartData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }

    public class DataListViewModel
    {
        public List<ExcelData> ExcelData { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 20;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public string? FilterBy { get; set; }
        public string? FilterValue { get; set; }
    }

    public class EditDataViewModel
    {
        public int Id { get; set; }
        public string? Ad { get; set; }
        public string? Soyad { get; set; }
        public string? TCKimlikNo { get; set; }
        public string? OgrenciNo { get; set; }
        public string? BasvuruYili { get; set; }
        public string? HareketlilikTipi { get; set; }
        public string? BasvuruTipi { get; set; }
        public string? OdemeTipi { get; set; }
        public string? Taksit { get; set; }
        public decimal? Odenecek { get; set; }
        public decimal? Odendiginde { get; set; }
        public DateTime? OdemeTarihi { get; set; }
        public string? Aciklama { get; set; }
        public decimal? OdemeOrani { get; set; }
        public string? KullaniciAdi { get; set; }
        public string? PasaportNo { get; set; }
        public DateTime? DogumTarihi { get; set; }
        public string? DogumYeri { get; set; }
        public string? Cinsiyet { get; set; }
        public string? AdresIl { get; set; }
        public string? AdresUlke { get; set; }
        public string? BankaHesapSahibi { get; set; }
        public string? BankaAdi { get; set; }
        public string? BankaSubeKodu { get; set; }
        public string? BankaSubeAdi { get; set; }
        public string? BankaHesapNumarasi { get; set; }
        public string? BankaIBANNo { get; set; }
        public string? FakulteAdi { get; set; }
        public string? BirimAdi { get; set; }
        public string? DiplomaDerecesi { get; set; }
        public string? Sinif { get; set; }
    }

    public class GetTableDataRequest
    {
        public string TableName { get; set; } = string.Empty;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; }
        public Dictionary<string, object>? Filters { get; set; }
    }

    public class DataRequest
    {
        public int? TableId { get; set; }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Beni hatırla")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Ad gereklidir")]
        [Display(Name = "Ad")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Soyad gereklidir")]
        [Display(Name = "Soyad")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "E-posta adresi gereklidir")]
        [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin")]
        [Display(Name = "E-posta")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre gereklidir")]
        [StringLength(100, ErrorMessage = "{0} en az {2} karakter uzunluğunda olmalıdır.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Şifre Onayı")]
        [Compare("Password", ErrorMessage = "Şifre ve şifre onayı eşleşmiyor.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class DynamicTableListViewModel
    {
        public List<DynamicTable> Tables { get; set; } = new List<DynamicTable>();
        public int TotalTables { get; set; }
        public int ProcessedTables { get; set; }
        public int PendingTables { get; set; }
    }

    public class DynamicTableDetailsViewModel
    {
        public DynamicTable Table { get; set; } = new();
        public List<object> Data { get; set; } = new();
        public int TotalRows { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }

    public class DynamicTableDataViewModel
    {
        public DynamicTable Table { get; set; } = new();
        public List<object> Data { get; set; } = new();
        public int TotalRows { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

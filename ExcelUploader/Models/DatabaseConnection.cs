using System.ComponentModel.DataAnnotations;

namespace ExcelUploader.Models
{
    public class DatabaseConnection
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Bağlantı Adı")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Sunucu Adı")]
        public string ServerName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Port")]
        public int Port { get; set; } = 1433;
        
        [Required]
        [Display(Name = "Veritabanı Adı")]
        public string DatabaseName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Kullanıcı Adı")]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Şifre")]
        public string Password { get; set; } = string.Empty;
        
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }
        
        [Display(Name = "Aktif mi")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }
        
        [Display(Name = "Son Test Tarihi")]
        public DateTime? LastTestDate { get; set; }
        
        [Display(Name = "Test Sonucu")]
        public bool? LastTestResult { get; set; }
    }
}

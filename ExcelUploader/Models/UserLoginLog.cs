using System.ComponentModel.DataAnnotations;
using ExcelUploader.Data;

namespace ExcelUploader.Models
{
    public class UserLoginLog
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(256)]
        public string UserEmail { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string? UserName { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Login, Logout, FailedLogin
        
        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        [MaxLength(45)]
        public string? IpAddress { get; set; }
        
        [MaxLength(500)]
        public string? UserAgent { get; set; }
        
        [MaxLength(1000)]
        public string? AdditionalInfo { get; set; }
        
        public bool IsSuccessful { get; set; }
        
        [MaxLength(500)]
        public string? FailureReason { get; set; }
        
        // Navigation property
        public virtual ApplicationUser? User { get; set; }
    }
}

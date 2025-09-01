using System.ComponentModel.DataAnnotations;
using ExcelUploader.Data;

namespace ExcelUploader.Models
{
    public class LoginLog
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public string UserName { get; set; } = string.Empty;
        
        [Required]
        public string Email { get; set; } = string.Empty;
        
        public DateTime LoginTime { get; set; }
        
        public string IpAddress { get; set; } = string.Empty;
        
        public string UserAgent { get; set; } = string.Empty;
        
        public bool IsSuccess { get; set; }
        
        public string? FailureReason { get; set; }
        
        public string? SessionId { get; set; }
        
        public DateTime? LogoutTime { get; set; }
        
        // Navigation property
        public virtual ApplicationUser? User { get; set; }
    }
}

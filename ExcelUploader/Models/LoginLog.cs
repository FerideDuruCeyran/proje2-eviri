using System.ComponentModel.DataAnnotations;
using ExcelUploader.Data;

namespace ExcelUploader.Models
{
    public class LoginLog
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string UserName { get; set; }
        
        [Required]
        public string Email { get; set; }
        
        public DateTime LoginTime { get; set; }
        
        public string IpAddress { get; set; }
        
        public string UserAgent { get; set; }
        
        public bool IsSuccess { get; set; }
        
        public string? FailureReason { get; set; }
        
        public string? SessionId { get; set; }
        
        public DateTime? LogoutTime { get; set; }
        
        // Navigation property
        public virtual ApplicationUser User { get; set; }
    }
}

namespace ExcelUploader.Models
{
    public class LoginLog
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
    }
}

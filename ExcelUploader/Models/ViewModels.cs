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
    }

    public class DashboardViewModel
    {
        public int TotalTables { get; set; }
        public int ProcessedTables { get; set; }
        public int PendingTables { get; set; }
        public int TotalRows { get; set; }
        public List<RecentTable> RecentTables { get; set; } = new();
    }

    public class RecentTable
    {
        public string TableName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }
    }
}

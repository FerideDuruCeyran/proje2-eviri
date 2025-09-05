using System.ComponentModel.DataAnnotations;

namespace ExcelUploader.Models
{
    public class DynamicTable
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TableName { get; set; } = string.Empty;
        
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime UploadDate { get; set; }
        
        public int RowCount { get; set; }
        
        public int ColumnCount { get; set; }
        
        public bool IsProcessed { get; set; }
    }
}

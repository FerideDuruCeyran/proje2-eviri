using System.ComponentModel.DataAnnotations;

namespace ExcelUploader.Models
{
    public class DynamicTable
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Tablo Adı")]
        public string TableName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Dosya Adı")]
        public string FileName { get; set; } = string.Empty;
        
        [Display(Name = "Yüklenme Tarihi")]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;
        
        [Display(Name = "Yükleyen Kullanıcı")]
        public string UploadedBy { get; set; } = string.Empty;
        
        [Display(Name = "Satır Sayısı")]
        public int RowCount { get; set; }
        
        [Display(Name = "Sütun Sayısı")]
        public int ColumnCount { get; set; }
        
        [Display(Name = "Açıklama")]
        public string? Description { get; set; }
        
        [Display(Name = "İşlendi mi")]
        public bool IsProcessed { get; set; } = false;
        
        [Display(Name = "İşlenme Tarihi")]
        public DateTime? ProcessedDate { get; set; }
        
        // Navigation property for table columns
        public virtual ICollection<TableColumn> Columns { get; set; } = new List<TableColumn>();
        
        // Navigation property for table data
        public virtual ICollection<TableData> Data { get; set; } = new List<TableData>();
    }

    public class TableColumn
    {
        public int Id { get; set; }
        
        [Required]
        [Display(Name = "Sütun Adı")]
        [MaxLength(128)]
        public string ColumnName { get; set; } = string.Empty;
        
        [Display(Name = "Görünen Ad")]
        public string DisplayName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Veri Tipi")]
        public string DataType { get; set; } = string.Empty;
        
        [Display(Name = "Sütun Sırası")]
        public int ColumnOrder { get; set; }
        
        [Display(Name = "Maksimum Uzunluk")]
        public int? MaxLength { get; set; }
        
        [Display(Name = "Zorunlu mu")]
        public bool IsRequired { get; set; } = false;
        
        [Display(Name = "Benzersiz mi")]
        public bool IsUnique { get; set; } = false;
        
        // Foreign key to DynamicTable
        public int DynamicTableId { get; set; }
        public virtual DynamicTable DynamicTable { get; set; } = null!;
    }

    public class TableData
    {
        public int Id { get; set; }
        
        [Display(Name = "Satır Numarası")]
        public int RowNumber { get; set; }
        
        [Display(Name = "Veri")]
        public string Data { get; set; } = string.Empty;
        
        [Display(Name = "Sütun")]
        public int ColumnId { get; set; }
        public virtual TableColumn Column { get; set; } = null!;
        
        // Foreign key to DynamicTable
        public int DynamicTableId { get; set; }
        public virtual DynamicTable DynamicTable { get; set; } = null!;
    }
}

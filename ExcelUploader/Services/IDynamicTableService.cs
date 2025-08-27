using ExcelUploader.Models;

namespace ExcelUploader.Services
{
    public interface IDynamicTableService
    {
        Task<DynamicTable> CreateTableFromExcelAsync(IFormFile file, string uploadedBy, string? description = null);
        Task<bool> CreateSqlTableAsync(DynamicTable dynamicTable);
        Task<bool> InsertDataAsync(DynamicTable dynamicTable, List<Dictionary<string, object>> data);
        Task<List<DynamicTable>> GetAllTablesAsync();
        Task<DynamicTable?> GetTableByIdAsync(int id);
        Task<DynamicTable?> GetTableByNameAsync(string tableName);
        Task<bool> DeleteTableAsync(int id);
        Task<List<Dictionary<string, object>>> GetTableDataAsync(string tableName, int page = 1, int pageSize = 50);
        Task<int> GetTableDataCountAsync(string tableName);
        Task<bool> UpdateTableDataAsync(string tableName, int rowId, Dictionary<string, object> data);
        Task<bool> DeleteTableDataAsync(string tableName, int rowId);
        Task<byte[]> ExportTableDataAsync(string tableName, string format = "xlsx");
    }
}

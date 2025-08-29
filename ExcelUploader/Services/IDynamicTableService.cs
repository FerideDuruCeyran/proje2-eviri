using ExcelUploader.Models;
using System.Data;

namespace ExcelUploader.Services
{
    public interface IDynamicTableService
    {
        Task<DynamicTable> CreateTableFromExcelAsync(IFormFile file, string uploadedBy, int? databaseConnectionId = null, string? description = null);
        Task<DynamicTable> CreateTableStructureAsync(IFormFile file, string uploadedBy, int? databaseConnectionId = null, string? description = null);
        Task<bool> InsertDataIntoTableAsync(int tableId, IFormFile file);
        Task<bool> CreateSqlTableAsync(DynamicTable dynamicTable, int? databaseConnectionId = null);
        Task<bool> InsertDataAsync(DynamicTable dynamicTable, List<Dictionary<string, object>> data);
        Task<List<DynamicTable>> GetAllTablesAsync();
        Task<DynamicTable?> GetTableByIdAsync(int id);
        Task<DynamicTable?> GetTableByNameAsync(string tableName);
        Task<bool> DeleteTableAsync(int id);
        Task<bool> DeleteTableAsync(string tableName);
        Task<List<object>> GetTableDataAsync(string tableName, int page = 1, int pageSize = 50);
        Task<int> GetTableRowCountAsync(string tableName);
        Task<int> GetTableDataCountAsync(string tableName);
        Task<bool> UpdateTableDataAsync(string tableName, int rowId, Dictionary<string, object> data);
        Task<bool> DeleteTableDataAsync(string tableName, int rowId);
        Task<byte[]> ExportTableDataAsync(string tableName, string format = "xlsx");
        Task<bool> ExecuteSqlQueryAsync(string sql, Dictionary<string, object>? parameters = null);
        Task<DataTable> ExecuteSqlQueryWithResultAsync(string sql, Dictionary<string, object>? parameters = null);
        Task<bool> TestDatabaseConnectionAsync();
        Task<Dictionary<string, string>> GetDatabaseInfoAsync();
    }
}

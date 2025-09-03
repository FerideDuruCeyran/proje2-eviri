using ExcelUploader.Models;
using System.Data;

namespace ExcelUploader.Services
{
    public interface IDynamicTableService
    {
        Task<DynamicTable> CreateTableFromExcelAsync(IFormFile file, string uploadedBy, int? databaseConnectionId = null, string? description = null);
        Task<DynamicTable> CreateTableStructureAsync(IFormFile file, string uploadedBy, int? databaseConnectionId = null, string? description = null);
        Task<bool> InsertDataIntoTableAsync(int tableId, IFormFile file, int? databaseConnectionId = null);
        Task<bool> CreateSqlTableAsync(DynamicTable dynamicTable, int? databaseConnectionId = null);
        Task<bool> InsertDataAsync(DynamicTable dynamicTable, List<Dictionary<string, object>> data, int? databaseConnectionId = null);
        Task<List<DynamicTable>> GetAllTablesAsync();
        Task<DynamicTable?> GetTableByIdAsync(int id);
        Task<DynamicTable?> GetTableByNameAsync(string tableName);
        Task<bool> DeleteTableAsync(int id);
        Task<bool> DeleteTableAsync(string tableName);
        Task<List<object>> GetTableDataAsync(string tableName, int page = 1, int pageSize = 50, int? databaseConnectionId = null);
        Task<int> GetTableRowCountAsync(string tableName);
        Task<int> GetTableDataCountAsync(string tableName);
        Task<bool> UpdateTableDataAsync(string tableName, int rowId, Dictionary<string, object> data);
        Task<bool> DeleteTableDataAsync(string tableName, int rowId);
        Task<byte[]> ExportTableDataAsync(string tableName, string format = "xlsx");
        Task<bool> ExecuteSqlQueryAsync(string sql, Dictionary<string, object>? parameters = null);
        Task<DataTable> ExecuteSqlQueryWithResultAsync(string sql, Dictionary<string, object>? parameters = null);

        Task<bool> TableExistsAsync(string tableName, int? databaseConnectionId = null);
        Task<int?> GetTableIdByNameAsync(string tableName);
        Task<int> InsertDataIntoExistingTableAsync(IFormFile file, string existingTableName, int? databaseConnectionId = null);
        Task<bool> CheckExactTableExistsAsync(string tableName, int? databaseConnectionId = null);
        Task<string?> FindExistingTableNameAsync(string tableName, int? databaseConnectionId = null);
        Task<(List<string> headers, List<string> dataTypes, List<Dictionary<string, object>> sampleData)> AnalyzeExcelFileAsync(IFormFile file);
        string SanitizeColumnName(string name);
        Task<List<Dictionary<string, object>>> ReadAllExcelDataAsync(IFormFile file);
        Task<(bool exists, DynamicTable? existingTable, string? actualTableName)> CheckTableExistsForInsertAsync(string tableName, int? databaseConnectionId = null);
        Task<(bool success, int insertedRows, string? errorMessage)> InsertDataIntoExistingTableWithSameNameAsync(IFormFile file, string tableName, int? databaseConnectionId = null);
    }
}

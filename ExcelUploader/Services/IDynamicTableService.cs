using ExcelUploader.Models;

namespace ExcelUploader.Services
{
    public interface IDynamicTableService
    {
        Task<ServiceResult> CreateTableFromExcelAsync(string tableName, List<string> headers, List<List<object>> rows, List<ColumnDataTypeAnalysis> columnDataTypes);
        Task<ServiceResult<List<Dictionary<string, object>>>> GetTableDataAsync(string tableName);
        Task<ServiceResult> DeleteTableAsync(string tableName);
    }
}

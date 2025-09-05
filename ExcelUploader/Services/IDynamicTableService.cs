using ExcelUploader.Models;
using Microsoft.AspNetCore.Http;

namespace ExcelUploader.Services
{
    public interface IDynamicTableService
    {
        Task<ServiceResult> CreateTableFromExcelAsync(string tableName, IFormFile file, string description);
        Task<ServiceResult<List<Dictionary<string, object>>>> GetTableDataAsync(string tableName);
        Task<ServiceResult> DeleteTableAsync(string tableName);
    }
}

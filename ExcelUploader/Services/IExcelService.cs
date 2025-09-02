using ExcelUploader.Models;

namespace ExcelUploader.Services
{
    public interface IExcelService
    {
        Task<List<ExcelData>> ProcessExcelFileAsync(IFormFile file, string uploadedBy);
        Task<byte[]> ExportToExcelAsync(List<ExcelData> data);
        Task<List<ExcelData>> GetDataByFileNameAsync(string fileName);
        Task<bool> ValidateExcelFileAsync(IFormFile file);
        Task<string> GetFileSummaryAsync(IFormFile file);
        Task<object> GetExcelPreviewAsync(IFormFile file);
        Task<object> GetExcelPreviewAsync(IFormFile file, int sheetIndex);
        Task<List<string>> GetSheetNamesAsync(IFormFile file);
    }
}

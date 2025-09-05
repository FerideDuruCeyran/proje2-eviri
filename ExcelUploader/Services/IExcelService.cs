using Microsoft.AspNetCore.Http;

namespace ExcelUploader.Services
{
    public interface IExcelService
    {
        Task<object> GetExcelPreviewAsync(IFormFile file);
        Task<object> GetExcelPreviewAsync(IFormFile file, int sheetIndex = 0);
        Task<List<string>> GetSheetNamesAsync(IFormFile file);
    }
}

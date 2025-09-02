namespace ExcelUploader.Services
{
    public interface IExcelAnalyzerService
    {
        Task<ExcelAnalysisResult> AnalyzeExcelFileAsync(IFormFile file);
        Task<ExcelAnalysisResult> AnalyzeExcelFileAsync(IFormFile file, int sheetIndex);
        Task<List<string>> GetSheetNamesAsync(IFormFile file);
    }
}

using ExcelUploader.Models;

namespace ExcelUploader.Services
{
    public interface IDataImportService
    {
        Task<bool> ImportDataAsync(List<ExcelData> data);
        Task<List<ExcelData>> GetAllDataAsync();
        Task<ExcelData?> GetDataByIdAsync(int id);
        Task<bool> UpdateDataAsync(ExcelData data);
        Task<bool> DeleteDataAsync(int id);
        Task<bool> DeleteDataByFileNameAsync(string fileName);
        Task<DashboardViewModel> GetDashboardDataAsync();
        Task<List<ExcelData>> SearchDataAsync(string searchTerm, string? filterBy = null, string? filterValue = null);
        Task<DataListViewModel> GetPaginatedDataAsync(int page, int pageSize, string? searchTerm = null, string? sortBy = null, string? sortOrder = null);
        Task<byte[]> ExportDataToExcelAsync(List<ExcelData> data);
        Task<int> GetTotalRecordCountAsync();
        Task<decimal> GetTotalGrantAmountAsync();
        Task<decimal> GetTotalPaidAmountAsync();
    }
}

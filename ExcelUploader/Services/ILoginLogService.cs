using ExcelUploader.Models;

namespace ExcelUploader.Services
{
    public interface ILoginLogService
    {
        Task<LoginLog> LogLoginAsync(string userId, string userName, string email, string ipAddress, string userAgent, bool isSuccess, string? failureReason = null);
        Task<LoginLog> LogLogoutAsync(string userId, string sessionId);
        Task<IEnumerable<LoginLog>> GetUserLoginLogsAsync(string userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<LoginLog>> GetAllLoginLogsAsync(int page = 1, int pageSize = 50);
        Task<int> GetTotalLoginLogsCountAsync();
        Task<int> GetUserLoginLogsCountAsync(string userId);
    }
}

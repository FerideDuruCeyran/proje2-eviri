using ExcelUploader.Models;

namespace ExcelUploader.Services
{
    public interface IUserLoginLogService
    {
        Task LogLoginAsync(string userId, string userEmail, string? userName, string ipAddress, string? userAgent, bool isSuccessful, string? failureReason = null);
        Task LogLogoutAsync(string userId, string userEmail, string? userName, string ipAddress, string? userAgent);
        Task<IEnumerable<UserLoginLog>> GetLoginLogsAsync(int page = 1, int pageSize = 50, string? userId = null, string? action = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<int> GetTotalLoginLogsCountAsync(string? userId = null, string? action = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<UserLoginLog>> GetUserLoginHistoryAsync(string userId, int page = 1, int pageSize = 20);
        Task<IEnumerable<UserLoginLog>> GetRecentLoginAttemptsAsync(int count = 10);
        Task<IEnumerable<UserLoginLog>> GetFailedLoginAttemptsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }
}

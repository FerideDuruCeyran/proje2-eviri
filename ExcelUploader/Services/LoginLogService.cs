using ExcelUploader.Data;
using ExcelUploader.Models;
using Microsoft.EntityFrameworkCore;

namespace ExcelUploader.Services
{
    public class LoginLogService : ILoginLogService
    {
        private readonly ApplicationDbContext _context;

        public LoginLogService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LoginLog> LogLoginAsync(string userId, string userName, string email, string ipAddress, string userAgent, bool isSuccess, string? failureReason = null)
        {
            var loginLog = new LoginLog
            {
                UserId = userId,
                UserName = userName,
                Email = email,
                LoginTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = isSuccess,
                FailureReason = failureReason,
                SessionId = Guid.NewGuid().ToString()
            };

            _context.LoginLogs.Add(loginLog);
            await _context.SaveChangesAsync();

            return loginLog;
        }

        public async Task<LoginLog> LogLogoutAsync(string userId, string sessionId)
        {
            var loginLog = await _context.LoginLogs
                .FirstOrDefaultAsync(l => l.UserId == userId && l.SessionId == sessionId && l.LogoutTime == null);

            if (loginLog != null)
            {
                loginLog.LogoutTime = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return loginLog;
        }

        public async Task<IEnumerable<LoginLog>> GetUserLoginLogsAsync(string userId, int page = 1, int pageSize = 20)
        {
            return await _context.LoginLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.LoginTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(l => l.User)
                .ToListAsync();
        }

        public async Task<IEnumerable<LoginLog>> GetAllLoginLogsAsync(int page = 1, int pageSize = 50)
        {
            return await _context.LoginLogs
                .OrderByDescending(l => l.LoginTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(l => l.User)
                .ToListAsync();
        }

        public async Task<int> GetTotalLoginLogsCountAsync()
        {
            return await _context.LoginLogs.CountAsync();
        }

        public async Task<int> GetUserLoginLogsCountAsync(string userId)
        {
            return await _context.LoginLogs.CountAsync(l => l.UserId == userId);
        }
    }
}

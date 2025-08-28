using ExcelUploader.Models;
using ExcelUploader.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

namespace ExcelUploader.Services
{
    public class UserLoginLogService : IUserLoginLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserLoginLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogLoginAsync(string userId, string userEmail, string? userName, string ipAddress, string? userAgent, bool isSuccessful, string? failureReason = null)
        {
            var log = new UserLoginLog
            {
                UserId = userId,
                UserEmail = userEmail,
                UserName = userName,
                Action = isSuccessful ? "Login" : "FailedLogin",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = isSuccessful,
                FailureReason = failureReason
            };

            _context.UserLoginLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task LogLogoutAsync(string userId, string userEmail, string? userName, string ipAddress, string? userAgent)
        {
            var log = new UserLoginLog
            {
                UserId = userId,
                UserEmail = userEmail,
                UserName = userName,
                Action = "Logout",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccessful = true
            };

            _context.UserLoginLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<UserLoginLog>> GetLoginLogsAsync(int page = 1, int pageSize = 50, string? userId = null, string? action = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.UserLoginLogs
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetTotalLoginLogsCountAsync(string? userId = null, string? action = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.UserLoginLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserId == userId);

            if (!string.IsNullOrEmpty(action))
                query = query.Where(l => l.Action == action);

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            return await query.CountAsync();
        }

        public async Task<IEnumerable<UserLoginLog>> GetUserLoginHistoryAsync(string userId, int page = 1, int pageSize = 20)
        {
            return await _context.UserLoginLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserLoginLog>> GetRecentLoginAttemptsAsync(int count = 10)
        {
            return await _context.UserLoginLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Take(count)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserLoginLog>> GetFailedLoginAttemptsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.UserLoginLogs
                .Include(l => l.User)
                .Where(l => !l.IsSuccessful);

            if (startDate.HasValue)
                query = query.Where(l => l.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(l => l.Timestamp <= endDate.Value);

            return await query
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
        }

        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return "Unknown";

            // Try to get IP from various headers
            var forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                return forwardedHeader.Split(',')[0].Trim();
            }

            var realIpHeader = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIpHeader))
            {
                return realIpHeader;
            }

            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string? GetUserAgent()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request.Headers["User-Agent"].FirstOrDefault();
        }
    }
}

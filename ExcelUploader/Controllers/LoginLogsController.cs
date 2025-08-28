using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExcelUploader.Services;
using ExcelUploader.Models;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoginLogsController : ControllerBase
    {
        private readonly IUserLoginLogService _userLoginLogService;
        private readonly ILogger<LoginLogsController> _logger;

        public LoginLogsController(IUserLoginLogService userLoginLogService, ILogger<LoginLogsController> logger)
        {
            _userLoginLogService = userLoginLogService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetLoginLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? userId = null,
            [FromQuery] string? action = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var logs = await _userLoginLogService.GetLoginLogsAsync(page, pageSize, userId, action, startDate, endDate);
                var totalCount = await _userLoginLogService.GetTotalLoginLogsCountAsync(userId, action, startDate, endDate);

                var result = new
                {
                    logs = logs.Select(l => new
                    {
                        l.Id,
                        l.UserId,
                        l.UserEmail,
                        l.UserName,
                        l.Action,
                        l.Timestamp,
                        l.IpAddress,
                        l.UserAgent,
                        l.IsSuccessful,
                        l.FailureReason
                    }),
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving login logs");
                return StatusCode(500, new { error = "Login günlükleri alınırken hata oluştu" });
            }
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentLoginAttempts([FromQuery] int count = 10)
        {
            try
            {
                var logs = await _userLoginLogService.GetRecentLoginAttemptsAsync(count);
                
                var result = logs.Select(l => new
                {
                    l.Id,
                    l.UserId,
                    l.UserEmail,
                    l.UserName,
                    l.Action,
                    l.Timestamp,
                    l.IpAddress,
                    l.UserAgent,
                    l.IsSuccessful,
                    l.FailureReason
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent login attempts");
                return StatusCode(500, new { error = "Son giriş denemeleri alınırken hata oluştu" });
            }
        }

        [HttpGet("failed")]
        public async Task<IActionResult> GetFailedLoginAttempts(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var logs = await _userLoginLogService.GetFailedLoginAttemptsAsync(startDate, endDate);
                
                var result = logs.Select(l => new
                {
                    l.Id,
                    l.UserId,
                    l.UserEmail,
                    l.UserName,
                    l.Action,
                    l.Timestamp,
                    l.IpAddress,
                    l.UserAgent,
                    l.IsSuccessful,
                    l.FailureReason
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving failed login attempts");
                return StatusCode(500, new { error = "Başarısız giriş denemeleri alınırken hata oluştu" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserLoginHistory(
            string userId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var logs = await _userLoginLogService.GetUserLoginHistoryAsync(userId, page, pageSize);
                
                var result = logs.Select(l => new
                {
                    l.Id,
                    l.UserId,
                    l.UserEmail,
                    l.UserName,
                    l.Action,
                    l.Timestamp,
                    l.IpAddress,
                    l.UserAgent,
                    l.IsSuccessful,
                    l.FailureReason
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user login history for user {UserId}", userId);
                return StatusCode(500, new { error = "Kullanıcı giriş geçmişi alınırken hata oluştu" });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetLoginStats()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var yesterday = today.AddDays(-1);
                var thisWeek = today.AddDays(-7);
                var thisMonth = today.AddDays(-30);

                var todayLogs = await _userLoginLogService.GetLoginLogsAsync(1, 1000, null, null, today, today.AddDays(1));
                var yesterdayLogs = await _userLoginLogService.GetLoginLogsAsync(1, 1000, null, null, yesterday, today);
                var thisWeekLogs = await _userLoginLogService.GetLoginLogsAsync(1, 1000, null, null, thisWeek, today.AddDays(1));
                var thisMonthLogs = await _userLoginLogService.GetLoginLogsAsync(1, 1000, null, null, thisMonth, today.AddDays(1));

                var stats = new
                {
                    today = new
                    {
                        total = todayLogs.Count(),
                        successful = todayLogs.Count(l => l.IsSuccessful),
                        failed = todayLogs.Count(l => !l.IsSuccessful)
                    },
                    yesterday = new
                    {
                        total = yesterdayLogs.Count(),
                        successful = yesterdayLogs.Count(l => l.IsSuccessful),
                        failed = yesterdayLogs.Count(l => !l.IsSuccessful)
                    },
                    thisWeek = new
                    {
                        total = thisWeekLogs.Count(),
                        successful = thisWeekLogs.Count(l => l.IsSuccessful),
                        failed = thisWeekLogs.Count(l => !l.IsSuccessful)
                    },
                    thisMonth = new
                    {
                        total = thisMonthLogs.Count(),
                        successful = thisMonthLogs.Count(l => l.IsSuccessful),
                        failed = thisMonthLogs.Count(l => !l.IsSuccessful)
                    }
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving login stats");
                return StatusCode(500, new { error = "Giriş istatistikleri alınırken hata oluştu" });
            }
        }
    }
}

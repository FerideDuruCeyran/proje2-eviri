using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ExcelUploader.Services;
using ExcelUploader.Models;
using System.Security.Claims;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LoginLogsController : ControllerBase
    {
        private readonly ILoginLogService _loginLogService;

        public LoginLogsController(ILoginLogService loginLogService)
        {
            _loginLogService = loginLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetLoginLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var logs = await _loginLogService.GetAllLoginLogsAsync(page, pageSize);
                var totalCount = await _loginLogService.GetTotalLoginLogsCountAsync();

                return Ok(new
                {
                    logs,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Giriş günlükleri alınırken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserLoginLogs(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var logs = await _loginLogService.GetUserLoginLogsAsync(userId, page, pageSize);
                var totalCount = await _loginLogService.GetUserLoginLogsCountAsync(userId);

                return Ok(new
                {
                    logs,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Kullanıcı giriş günlükleri alınırken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("my-logs")]
        public async Task<IActionResult> GetMyLoginLogs([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "Kullanıcı kimliği bulunamadı" });
                }

                var logs = await _loginLogService.GetUserLoginLogsAsync(userId, page, pageSize);
                var totalCount = await _loginLogService.GetUserLoginLogsCountAsync(userId);

                return Ok(new
                {
                    logs,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Giriş günlükleriniz alınırken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetLoginStatistics()
        {
            try
            {
                var totalLogs = await _loginLogService.GetTotalLoginLogsCountAsync();
                
                // Burada daha detaylı istatistikler eklenebilir
                // Örneğin: başarılı/başarısız giriş sayıları, günlük/haftalık trendler vb.

                return Ok(new
                {
                    totalLogs,
                    message = "Giriş günlüğü istatistikleri başarıyla alındı"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "İstatistikler alınırken hata oluştu", error = ex.Message });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var totalLogs = await _loginLogService.GetTotalLoginLogsCountAsync();
                
                // Bu endpoint /stats için alias olarak çalışır
                return Ok(new
                {
                    totalLogs,
                    message = "Giriş günlüğü istatistikleri başarıyla alındı"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "İstatistikler alınırken hata oluştu", error = ex.Message });
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ExcelUploader.Models;
using ExcelUploader.Data;
using ExcelUploader.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IUserLoginLogService _userLoginLogService;
        private readonly ILoginLogService _loginLogService;
        private readonly IJwtService _jwtService;

        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            ILogger<AccountController> logger,
            IUserLoginLogService userLoginLogService,
            ILoginLogService loginLogService,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _userLoginLogService = userLoginLogService;
            _loginLogService = loginLogService;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            _logger.LogInformation("Login attempt received for email: {Email}", model?.Email);
            
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                _logger.LogInformation("User lookup result: {UserFound}", user != null);
                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in: {Email}", model.Email);
                    
                    // Generate JWT token
                    var token = _jwtService.GenerateToken(user ?? throw new InvalidOperationException("User not found"));
                    
                    // Log successful login with LoginLogService
                    if (user != null)
                    {
                        await _loginLogService.LogLoginAsync(
                            user.Id, 
                            $"{user.FirstName} {user.LastName}", 
                            user.Email!, 
                            ipAddress, 
                            userAgent ?? string.Empty, 
                            true);
                    }
                    
                    return Ok(new { 
                        message = "Giriş başarılı", 
                        token = token,
                        user = new { 
                            id = user?.Id, 
                            email = user?.Email, 
                            firstName = user?.FirstName, 
                            lastName = user?.LastName 
                        } 
                    });
                }
                else
                {
                    // Log failed login attempt with LoginLogService
                    if (user != null)
                    {
                        await _loginLogService.LogLoginAsync(
                            user.Id, 
                            $"{user.FirstName} {user.LastName}", 
                            user.Email!, 
                            ipAddress, 
                            userAgent ?? string.Empty, 
                            false, 
                            "Geçersiz şifre");
                    }
                    else
                    {
                        // Log failed login for non-existent user
                        await _loginLogService.LogLoginAsync(
                            "unknown", 
                            "Bilinmeyen Kullanıcı", 
                            model.Email, 
                            ipAddress, 
                            userAgent ?? string.Empty, 
                            false, 
                            "Kullanıcı bulunamadı");
                    }
                    
                    return BadRequest(new { message = "Geçersiz giriş denemesi." });
                }
            }

            return BadRequest(new { message = "Geçersiz model verisi", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password: {Email}", model.Email);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return Ok(new { message = "Kullanıcı başarıyla oluşturuldu", user = new { id = user.Id, email = user.Email } });
                }

                return BadRequest(new { message = "Kullanıcı oluşturulamadı", errors = result.Errors.Select(e => e.Description) });
            }

            return BadRequest(new { message = "Geçersiz model verisi", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            
            // Log logout with LoginLogService
            if (user != null)
            {
                // For now, we'll use a placeholder session ID since ApplicationUser doesn't have SessionId
                await _loginLogService.LogLogoutAsync(user.Id, "session-" + user.Id);
            }
            
            return Ok(new { message = "Çıkış başarılı" });
        }

        [HttpGet("access-denied")]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return Unauthorized(new { message = "Bu sayfaya erişim yetkiniz yok" });
        }

        [HttpGet("verify")]
        [Authorize]
        public async Task<IActionResult> Verify()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Geçersiz token" });
            }

            return Ok(new { 
                id = user.Id, 
                email = user.Email, 
                firstName = user.FirstName, 
                lastName = user.LastName,
                createdAt = user.CreatedAt,
                isActive = user.IsActive
            });
        }

        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            return Ok(new { 
                user = new { 
                    id = user.Id, 
                    email = user.Email, 
                    firstName = user.FirstName, 
                    lastName = user.LastName,
                    createdAt = user.CreatedAt,
                    isActive = user.IsActive
                } 
            });
        }

        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> Stats()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            // For now, return placeholder stats
            // You can implement actual stats calculation here
            return Ok(new { 
                totalUploads = 0,
                totalRecords = 0,
                totalTables = 0,
                lastLogin = user.CreatedAt
            });
        }

        [HttpGet("test-users")]
        [AllowAnonymous]
        public async Task<IActionResult> TestUsers()
        {
            try
            {
                var users = await _userManager.Users.Take(10).ToListAsync();
                var userList = users.Select(u => new { 
                    id = u.Id, 
                    email = u.Email, 
                    firstName = u.FirstName, 
                    lastName = u.LastName,
                    isActive = u.IsActive
                }).ToList();
                
                return Ok(new { 
                    totalUsers = users.Count,
                    users = userList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { error = "Kullanıcılar alınırken hata oluştu" });
            }
        }

        private string GetClientIpAddress()
        {
            // Try to get IP from various headers
            var forwardedHeader = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                return forwardedHeader.Split(',')[0].Trim();
            }

            var realIpHeader = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIpHeader))
            {
                return realIpHeader;
            }

            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string? GetUserAgent()
        {
            return Request.Headers["User-Agent"].FirstOrDefault();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}

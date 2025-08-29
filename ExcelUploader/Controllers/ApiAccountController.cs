using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ExcelUploader.Models;
using ExcelUploader.Data;
using ExcelUploader.Services;

namespace ExcelUploader.Controllers
{
    [ApiController]
    [Route("api/account")]
    public class ApiAccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<ApiAccountController> _logger;
        private readonly IUserLoginLogService _userLoginLogService;
        private readonly IJwtService _jwtService;

        public ApiAccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            ILogger<ApiAccountController> logger,
            IUserLoginLogService userLoginLogService,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _userLoginLogService = userLoginLogService;
            _jwtService = jwtService;
        }

        // API endpoint for login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            _logger.LogInformation("Login attempt received for email: {Email}", model?.Email ?? "null");
            
            if (model == null)
            {
                _logger.LogWarning("Login model is null");
                return BadRequest(new { message = "Model null" });
            }
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login model validation failed: {Errors}", string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(new { message = "Geçersiz model" });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in via API: {Email}", model.Email);
                
                // Log successful login
                if (user != null)
                {
                    await _userLoginLogService.LogLoginAsync(
                        user.Id, 
                        user.Email!, 
                        $"{user.FirstName} {user.LastName}", 
                        ipAddress, 
                        userAgent, 
                        true);
                }

                // Generate JWT token
                var token = _jwtService.GenerateToken(user!);
                var userInfo = new
                {
                    id = user?.Id,
                    email = user?.Email,
                    firstName = user?.FirstName,
                    lastName = user?.LastName
                };

                return Ok(new { 
                    success = true, 
                    message = "Giriş başarılı",
                    user = userInfo,
                    token = token
                });
            }
            else
            {
                // Log failed login attempt
                if (user != null)
                {
                    await _userLoginLogService.LogLoginAsync(
                        user.Id, 
                        user.Email!, 
                        $"{user.FirstName} {user.LastName}", 
                        ipAddress, 
                        userAgent, 
                        false, 
                        "Invalid password");
                }
                else
                {
                    // Log failed login for non-existent user
                    await _userLoginLogService.LogLoginAsync(
                        "unknown", 
                        model.Email, 
                        null, 
                        ipAddress, 
                        userAgent, 
                        false, 
                        "User not found");
                }

                return BadRequest(new { 
                    success = false, 
                    message = "Geçersiz e-posta veya şifre" 
                });
            }
        }

        // API endpoint for register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Geçersiz model" });
            }

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
                _logger.LogInformation("User created via API: {Email}", model.Email);

                return Ok(new { 
                    success = true, 
                    message = "Kayıt başarılı" 
                });
            }

            var errors = result.Errors.Select(e => e.Description);
            return BadRequest(new { 
                success = false, 
                message = "Kayıt başarısız",
                errors = errors
            });
        }

        // API endpoint for profile
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı" });
            }

            var profile = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                createdAt = user.CreatedAt,
                isActive = user.IsActive
            };

            return Ok(profile);
        }

        // API endpoint for token verification
        [HttpGet("verify")]
        [Authorize]
        public async Task<IActionResult> VerifyToken()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "Geçersiz token" });
            }

            var profile = new
            {
                id = user.Id,
                email = user.Email,
                firstName = user.FirstName,
                lastName = user.LastName,
                createdAt = user.CreatedAt,
                isActive = user.IsActive
            };

            return Ok(profile);
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
    }
}

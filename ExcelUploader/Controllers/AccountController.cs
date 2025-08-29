using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using ExcelUploader.Models;
using ExcelUploader.Data;
using ExcelUploader.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace ExcelUploader.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IUserLoginLogService _userLoginLogService;

        public AccountController(
            UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            ILogger<AccountController> logger,
            IUserLoginLogService userLoginLogService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _userLoginLogService = userLoginLogService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                var ipAddress = GetClientIpAddress();
                var userAgent = GetUserAgent();

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in: {Email}", model.Email);
                    
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
                    
                    return RedirectToLocal(returnUrl);
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
                    
                    ModelState.AddModelError(string.Empty, "Geçersiz giriş denemesi.");
                    return View(model);
                }
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

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
                    return RedirectToLocal(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            
            // Log logout
            if (user != null)
            {
                await _userLoginLogService.LogLogoutAsync(
                    user.Id, 
                    user.Email!, 
                    $"{user.FirstName} {user.LastName}", 
                    ipAddress, 
                    userAgent);
            }
            
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult Profile()
        {
            return View();
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

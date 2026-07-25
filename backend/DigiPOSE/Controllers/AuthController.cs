using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using BC = BCrypt.Net.BCrypt;

namespace DigiPOSE.Controllers
{
    public class AuthController : Controller
    {
        private readonly DigiPoseDbContext _context;

        public AuthController(DigiPoseDbContext context)
        {
            _context = context;
        }

        // GET: /Auth/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return LocalRedirect(returnUrl ?? "/Home/DashboardRouter");
            }

            ViewBag.ReturnUrl = returnUrl ?? "/Home/DashboardRouter";
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra tài khoản tồn tại (không xét IsActive ngay ở đây để lấy thông báo chi tiết)
                var user = await _context.Users
                    .Include(u => u.Role)
                        .ThenInclude(r => r!.PermissionRoles!)
                            .ThenInclude(pr => pr.Permission)
                    .Include(u => u.Branch)
                    .SingleOrDefaultAsync(u => u.UserName == model.Username);

                if (user == null || !BC.Verify(model.Password, user.PasswordHash))
                {
                    TempData["ErrorMessage"] = "Invalid username or password.";
                    return View(model);
                }

                if (!user.IsActive)
                {
                    TempData["ErrorMessage"] = "Your account is pending administrator approval.";
                    return View(model);
                }

                // Khởi tạo danh sách Claims cho người dùng
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim("UserId", user.UserId.ToString()),
                    new System.Security.Claims.Claim(ClaimTypes.Name, user.UserName),
                    new System.Security.Claims.Claim("FullName", user.FullName ?? user.UserName),
                    new System.Security.Claims.Claim("BranchId", user.BranchId.ToString()),
                    new System.Security.Claims.Claim("BranchName", user.Branch?.BranchName ?? "N/A"),
                    new System.Security.Claims.Claim(ClaimTypes.Role, user.Role?.RoleName ?? "User")
                };

                // Load permission claims for Policy-based authorization
                if (user.Role?.PermissionRoles != null)
                {
                    foreach (var pr in user.Role.PermissionRoles)
                    {
                        if (pr.Permission != null)
                        {
                            claims.Add(new System.Security.Claims.Claim("Permission", pr.Permission.PermissionName));
                        }
                    }
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
                };

                // Đăng nhập hệ thống (Ghi Cookie Authentication)
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return LocalRedirect(model.ReturnUrl ?? "/Home/DashboardRouter");
            }

            return View(model);
        }

        // GET: /Auth/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home", new { Area = "Administrator" });
            }
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var exists = await _context.Users.AnyAsync(u => u.UserName == model.Username || u.Email == model.Email);
                if (exists)
                {
                    TempData["ErrorMessage"] = "Username or Email already exists.";
                    return View(model);
                }

                var newUser = new User
                {
                    UserName = model.Username,
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = BC.HashPassword(model.Password),
                    IsActive = false,
                    RoleId = 99,
                    BranchId = 1 // Default HQ branch
                };
                
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "Registration successful! Please wait for administrator account approval.";
                return RedirectToAction("Login");
            }
            return View(model);
        }

        // GET: /Auth/ChangePassword
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Auth/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userIdStr = User.FindFirstValue("UserId");
                if (int.TryParse(userIdStr, out int userId))
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null && BC.Verify(model.CurrentPassword, user.PasswordHash))
                    {
                        user.PasswordHash = BC.HashPassword(model.NewPassword);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Password changed successfully.";
                        return RedirectToAction("Index", "Home", new { Area = "Administrator" });
                    }
                    TempData["ErrorMessage"] = "Incorrect current password.";
                }
            }
            return View(model);
        }

        // GET: /Auth/ForgotPassword
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Auth/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.SingleOrDefaultAsync(u => u.Email == model.Email);
                if (user != null)
                {
                    // Logic to send reset email goes here. For now, we simulate success.
                    TempData["SuccessMessage"] = "Password reset instructions have been sent to your email.";
                    return RedirectToAction("Login");
                }
                TempData["ErrorMessage"] = "Email not found.";
            }
            return View(model);
        }

        // GET: /Auth/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Auth");
        }

        // GET: /Auth/Forbidden
        [AllowAnonymous]
        public IActionResult Forbidden()
        {
            return View();
        }
    }
}

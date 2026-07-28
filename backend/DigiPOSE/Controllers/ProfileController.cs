using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;

namespace DigiPOSE.Controllers
{
    /// <summary>
    /// Phase 7 - Self-Service User Profile Management Controller.
    /// Manages employee and customer credential updates, reward balances, and secure avatar file uploading.
    /// </summary>
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly DigiPoseDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProfileController(DigiPoseDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: /Profile/Index
        // Displays detailed profile overview with telemetry and assigned operational authority.
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User identity entity not located within active cluster.";
                return RedirectToAction("Login", "Auth");
            }

            // Retrieve associated customer/employee order histories for Self-Service Portal (My Orders tab)
            var userOrders = await _context.Orders
                .Include(o => o.OrderStatus)
                .AsNoTracking()
                .Where(o => o.UserId == userId || (!string.IsNullOrEmpty(user.PhoneNumber) && o.SnapshotCustomerPhone == user.PhoneNumber))
                .OrderByDescending(o => o.CreatedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.UserOrders = userOrders;

            return View(user);
        }

        // GET: /Profile/Edit
        // Renders self-service modification interface.
        public async Task<IActionResult> Edit()
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: /Profile/Edit
        // Executes credential persistence and cryptographic avatar storage.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model, IFormFile? ImageUpload)
        {
            var userIdStr = User.FindFirstValue("UserId");
            if (!int.TryParse(userIdStr, out int userId) || userId != model.UserId)
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            // Execute Secure Avatar File Uploading Protocol
            if (ImageUpload != null && ImageUpload.Length > 0)
            {
                // Validate file dimensions and allowable image formats to prevent script insertion
                if (ImageUpload.Length > 5 * 1024 * 1024) // 5MB limit
                {
                    ModelState.AddModelError("ImageUpload", "Avatar file size must not exceed 5MB.");
                    return View(user);
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
                var extension = Path.GetExtension(ImageUpload.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("ImageUpload", "Only JPG, PNG, WEBP, or GIF formats are authorized for system avatars.");
                    return View(user);
                }

                string uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                // Clean up previous image file from physical storage to prevent storage accumulation
                if (!string.IsNullOrEmpty(user.ImageUrl))
                {
                    string oldPhysicalPath = Path.Combine(uploadFolder, user.ImageUrl);
                    if (System.IO.File.Exists(oldPhysicalPath))
                    {
                        try { System.IO.File.Delete(oldPhysicalPath); }
                        catch { /* Ignore IO lock anomalies during file purging */ }
                    }
                }

                // Generate non-guessable secure cryptographic file name
                string newFileName = $"usr-{user.UserId}-{Guid.NewGuid():N}{extension}";
                string savePath = Path.Combine(uploadFolder, newFileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await ImageUpload.CopyToAsync(stream);
                }

                user.ImageUrl = newFileName;
            }

            // Persist valid editable profile parameters
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Email = model.Email;

            try
            {
                _context.Update(user);
                await _context.SaveChangesAsync();

                // Refresh existing authentication claims with updated identity parameters
                var updatedUser = await _context.Users
                    .Include(u => u.Role)
                        .ThenInclude(r => r!.PermissionRoles!)
                            .ThenInclude(pr => pr.Permission)
                    .Include(u => u.Branch)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == user.UserId);

                if (updatedUser != null)
                {
                    var claims = new List<System.Security.Claims.Claim>
                    {
                        new System.Security.Claims.Claim("UserId", updatedUser.UserId.ToString()),
                        new System.Security.Claims.Claim(ClaimTypes.Name, updatedUser.UserName),
                        new System.Security.Claims.Claim("FullName", updatedUser.FullName ?? updatedUser.UserName),
                        new System.Security.Claims.Claim("BranchId", updatedUser.BranchId.ToString()),
                        new System.Security.Claims.Claim("BranchName", updatedUser.Branch?.BranchName ?? "N/A"),
                        new System.Security.Claims.Claim(ClaimTypes.Role, updatedUser.Role?.RoleName ?? "User"),
                        new System.Security.Claims.Claim("AvatarUrl", updatedUser.ImageUrl ?? "")
                    };
                    if (updatedUser.Role?.PermissionRoles != null)
                    {
                        foreach (var pr in updatedUser.Role.PermissionRoles)
                        {
                            if (pr.Permission != null)
                            {
                                claims.Add(new System.Security.Claims.Claim("Permission", pr.Permission.PermissionName));
                            }
                        }
                    }
                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                }

                TempData["SuccessMessage"] = "Profile specifications and avatar telemetry updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Database persistence error occurred during profile synchronization: " + ex.Message);
                return View(user);
            }
        }
    }
}

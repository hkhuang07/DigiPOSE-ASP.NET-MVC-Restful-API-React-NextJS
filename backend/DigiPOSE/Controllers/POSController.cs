using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using System.Security.Claims;

namespace DigiPOSE.Controllers
{
    [Authorize]
    public class PosController : Controller
    {
        private readonly DigiPoseDbContext _context;

        public PosController(DigiPoseDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? "User";
            // 1. Khách (Role = User) thì không có chức năng POS
            if (roleClaim == "User")
            {
                return RedirectToAction("Forbidden", "Auth");
            }

            var userIdStr = User.FindFirst("UserId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(userIdStr, out int userId);

            bool isSuperAdmin = (roleClaim == "Super Admin" || roleClaim == "Admin" || roleClaim == "Administrator" || userId == 1);
            ViewBag.IsSuperAdmin = isSuperAdmin;
            ViewBag.AllTenants = new List<Tenant>();
            ViewBag.IsQuarantined = false;
            ViewBag.AuthorizedTenantId = 0;
            ViewBag.AuthorizedTenantName = "";

            if (isSuperAdmin)
            {
                // Super admin hiển thị thêm select Tenant cho họ quản lý POS mọi tenant
                var tenants = await _context.Tenants.Where(b => b.IsActive).ToListAsync();
                ViewBag.AllTenants = tenants;
                
                var currentTenantClaim = User.FindFirst("TenantId")?.Value;
                if (int.TryParse(currentTenantClaim, out int claimTenantId) && tenants.Any(b => b.TenantId == claimTenantId))
                {
                    var selected = tenants.First(b => b.TenantId == claimTenantId);
                    ViewBag.AuthorizedTenantId = selected.TenantId;
                    ViewBag.AuthorizedTenantName = selected.TenantName;
                }
                else
                {
                    var defaultTenant = tenants.FirstOrDefault() ?? await _context.Tenants.FindAsync(1);
                    ViewBag.AuthorizedTenantId = defaultTenant?.TenantId ?? 1;
                    ViewBag.AuthorizedTenantName = defaultTenant?.TenantName ?? "HQ Main Store";
                }
            }
            else
            {
                // Verify tenant ownership strictly in UserTenants table
                // Khóa cứng (Quarantine Lockdown) đối với tài khoản không có quyền sở hữu trạm cụ thể tại bảng UserTenants thay vì fallback
                var userTenant = await _context.UserTenants
                    .Include(ub => ub.Tenant)
                    .Where(ub => ub.UserId == userId && ub.IsActive)
                    .FirstOrDefaultAsync();

                if (userTenant == null || userTenant.Tenant == null)
                {
                    // Zero-Trust Quarantine Lockdown: Do NOT fallback to Tenant 1
                    ViewBag.IsQuarantined = true;
                    ViewBag.AuthorizedTenantId = 0;
                    ViewBag.AuthorizedTenantName = "QUARANTINED / NO TENANT ROUTING";
                }
                else
                {
                    ViewBag.AuthorizedTenantId = userTenant.TenantId;
                    ViewBag.AuthorizedTenantName = userTenant.Tenant.TenantName;
                }
            }

            return View();
        }
    }
}


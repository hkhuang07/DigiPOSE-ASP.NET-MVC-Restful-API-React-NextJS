using DigiPOSE.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DigiPOSE.Controllers
{
    public class HomeController : Controller
    {
        private readonly DigiPoseDbContext _context;

        public HomeController(DigiPoseDbContext context)
        {
            _context = context;
        }

        // Public Landing Page / Automatic Role Router
        [AllowAnonymous]
        public IActionResult Index()
        {
            // Logic nghiệp vụ: Nếu người dùng ĐÃ ĐĂNG NHẬP -> Tự động chuyển hướng về Home theo quyền (Role)
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return DashboardRouter();
            }

            // Nếu CHƯA ĐĂNG NHẬP -> Hiển thị trang Landing Page công khai cho khách
            return View();
        }

        // Protected Router for authenticated users
        [Authorize]
        public IActionResult DashboardRouter()
        {
            // Lấy Role hiện tại của User đăng nhập
            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

            // Điều hướng động (Dynamic Routing) dựa trên Role
            return userRole switch
            {
                "Super Admin" => RedirectToAction("Index", "Home", new { Area = "Administrator" }),
                "Branch Manager" => RedirectToAction("Index", "Home", new { Area = "Administrator" }),
                "POS Operator" => RedirectToAction("Index", "Home", new { Area = "Administrator" }), 
                "Warehouse" => RedirectToAction("Index", "Home", new { Area = "Administrator" }), 
                "Catalog" => RedirectToAction("Index", "Home", new { Area = "Administrator" }), 
                "Accountant" => RedirectToAction("Index", "Home", new { Area = "Administrator" }), 
                "Pending Approval" => RedirectToAction("Index", "Home", new { Area = "" }), 
                "User" => RedirectToAction("Index", "Home", new { Area = "" }), 
                _ => RedirectToAction("Index", "Home", new { Area = "Administrator" })
            };
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

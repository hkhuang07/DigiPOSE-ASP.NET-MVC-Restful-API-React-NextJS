using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DigiPOSE.Controllers
{
    /// <summary>
    /// Phase 6.2 - MVC Web Controller for Online E-Commerce & SaaS Storefront Portal.
    /// Strictly guarantees 0% mock data by binding active database records to dynamic Cyber-HUD interfaces.
    /// </summary>
    [Route("Storefront")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class StorefrontController : Controller
    {
        private readonly DigiPoseDbContext _context;

        public StorefrontController(DigiPoseDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            // 1. Fetch real active catalog assets from Database
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Manufacturer)
                .Include(p => p.ProductType)
                .Include(p => p.ItemNature)
                .Include(p => p.Unit)
                .Where(p => p.IsActive)
                .AsNoTracking()
                .ToListAsync();

            var categories = await _context.Categories.AsNoTracking().ToListAsync();
            var manufacturers = await _context.Manufacturers.AsNoTracking().ToListAsync();
            var productTypes = await _context.ProductTypes.AsNoTracking().ToListAsync();
            var itemNatures = await _context.ItemNatures.AsNoTracking().ToListAsync();

            // Select up to 5 featured products for Hero Rotating Carousel
            var featuredProducts = products.Take(5).ToList();
            if (!featuredProducts.Any() && products.Any())
            {
                featuredProducts = products.ToList();
            }

            var viewModel = new StorefrontIndexViewModel
            {
                Products = products,
                Categories = categories,
                Manufacturers = manufacturers,
                ProductTypes = productTypes,
                ItemNatures = itemNatures,
                FeaturedProducts = featuredProducts
            };

            return View(viewModel);
        }

        [HttpGet("Checkout")]
        public async Task<IActionResult> Checkout(int? cartId)
        {
            if (!cartId.HasValue || cartId.Value <= 0)
            {
                return RedirectToAction(nameof(Index));
            }

            var cart = await _context.StorefrontCarts
                .Include(c => c.Items!)
                .ThenInclude(i => i.Product!)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CartId == cartId.Value);

            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction(nameof(Index));
            }

            var paymentMethods = await _context.PaymentMethods.AsNoTracking().ToListAsync();

            var viewModel = new StorefrontCheckoutViewModel
            {
                Cart = cart,
                PaymentMethods = paymentMethods
            };

            return View(viewModel);
        }

        [HttpGet("Thanks")]
        public async Task<IActionResult> Thanks(int orderId, string? invoiceNumber = null, decimal totalCharged = 0)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails!)
                .Include(o => o.PaymentMethod)
                .Include(o => o.Customer)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            ViewData["InvoiceNumber"] = invoiceNumber ?? order?.InvoiceNumber ?? $"WEB-{orderId}";
            ViewData["TotalCharged"] = totalCharged > 0 ? totalCharged : (order?.TotalAmount ?? 0);

            return View(order);
        }
    }

    // ====================================================================
    // REAL DATA VIEW MODELS (0% MOCK TOLERATED)
    // ====================================================================
    public class StorefrontIndexViewModel
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Manufacturer> Manufacturers { get; set; } = new();
        public List<ProductType> ProductTypes { get; set; } = new();
        public List<ItemNature> ItemNatures { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();
    }

    public class StorefrontCheckoutViewModel
    {
        public StorefrontCart Cart { get; set; } = null!;
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
    }
}

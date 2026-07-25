using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DigiPOSE.Controllers
{
    /// <summary>
    /// Phase 6.2 - MVC Web Controller for Online E-Commerce & SaaS Storefront Portal.
    /// Resolves navigation endpoints and bridges ASP.NET Core MVC interface with API-Driven Client operations.
    /// </summary>
    [Route("Storefront")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class StorefrontController : Controller
    {
        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }
    }
}

using System.Threading.Tasks;
using DigiPOSE.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DigiPOSE.Controllers.Api
{
    [ApiController]
    [Route("api/gis")]
    public class GisController : ControllerBase
    {
        private readonly IGisResilienceService _gisService;
        private readonly ILogger<GisController> _logger;

        public GisController(IGisResilienceService gisService, ILogger<GisController> logger)
        {
            _gisService = gisService;
            _logger = logger;
        }

        [HttpGet("provinces")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetProvinces()
        {
            _logger.LogDebug(">>> [GIS_API_PROXY]: Client requested national province topology");
            var json = await _gisService.GetProvincesAsync();
            return Content(json, "application/json");
        }

        [HttpGet("p/{provinceCode}")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetDistricts(string provinceCode)
        {
            if (string.IsNullOrWhiteSpace(provinceCode))
            {
                return BadRequest(new { error = "Invalid province code" });
            }

            var json = await _gisService.GetDistrictsByProvinceAsync(provinceCode);
            return Content(json, "application/json");
        }

        [HttpGet("d/{districtCode}")]
        [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> GetWards(string districtCode)
        {
            if (string.IsNullOrWhiteSpace(districtCode))
            {
                return BadRequest(new { error = "Invalid district code" });
            }

            var json = await _gisService.GetWardsByDistrictAsync(districtCode);
            return Content(json, "application/json");
        }
    }
}

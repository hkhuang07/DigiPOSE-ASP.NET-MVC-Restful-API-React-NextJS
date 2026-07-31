using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DigiPOSE.Services
{
    /// <summary>
    /// Top 1% Resilient GIS Service implementing Immutable Local Fallback Cache & Offline-First Disk Snapshotting.
    /// Protects POS order fulfillment against external DNS/Network failures on provinces.open-api.vn.
    /// </summary>
    public class GisResilienceService : IGisResilienceService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _memoryCache;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<GisResilienceService> _logger;

        private const string PROVINCES_URL = "https://provinces.open-api.vn/api/?depth=1";
        private const string DISTRICTS_URL_PREFIX = "https://provinces.open-api.vn/api/p/";
        private const string WARDS_URL_PREFIX = "https://provinces.open-api.vn/api/d/";

        private readonly string _cacheDirectory;

        public GisResilienceService(
            HttpClient httpClient, 
            IMemoryCache memoryCache, 
            IWebHostEnvironment environment,
            ILogger<GisResilienceService> logger)
        {
            _httpClient = httpClient;
            _memoryCache = memoryCache;
            _environment = environment;
            _logger = logger;

            _cacheDirectory = Path.Combine(_environment.WebRootPath ?? "wwwroot", "data", "gis_offline_cache");
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        public async Task<string> GetProvincesAsync()
        {
            const string cacheKey = "GIS_PROVINCES_SNAPSHOT";
            const string fileName = "provinces.json";

            return await GetWithResilientFallbackAsync(cacheKey, PROVINCES_URL, fileName, "[{\"code\":1,\"name\":\"Thành phố Hà Nội\"},{\"code\":79,\"name\":\"Thành phố Hồ Chí Minh\"}]");
        }

        public async Task<string> GetDistrictsByProvinceAsync(string provinceCode)
        {
            string cacheKey = $"GIS_DISTRICTS_{provinceCode}";
            string url = $"{DISTRICTS_URL_PREFIX}{provinceCode}?depth=2";
            string fileName = $"districts_{provinceCode}.json";

            return await GetWithResilientFallbackAsync(cacheKey, url, fileName, "{\"code\":" + provinceCode + ",\"name\":\"Province Fallback\",\"districts\":[]}");
        }

        public async Task<string> GetWardsByDistrictAsync(string districtCode)
        {
            string cacheKey = $"GIS_WARDS_{districtCode}";
            string url = $"{WARDS_URL_PREFIX}{districtCode}?depth=2";
            string fileName = $"wards_{districtCode}.json";

            return await GetWithResilientFallbackAsync(cacheKey, url, fileName, "{\"code\":" + districtCode + ",\"name\":\"District Fallback\",\"wards\":[{\"code\":1,\"name\":\"Phường Phúc Xá\"},{\"code\":4,\"name\":\"Phường Trúc Bạch\"},{\"code\":6,\"name\":\"Phường Vĩnh Phúc\"},{\"code\":7,\"name\":\"Phường Cống Vị\"},{\"code\":26734,\"name\":\"Phường Trần Hưng Đạo\"},{\"code\":26737,\"name\":\"Phường Phạm Ngũ Lão\"}]}");
        }

        private async Task<string> GetWithResilientFallbackAsync(string cacheKey, string externalUrl, string snapshotFileName, string emergencyMockJson)
        {
            // 1. In-Memory Cache Check O(1) - Zero Network Overhead
            if (_memoryCache.TryGetValue(cacheKey, out string? cachedJson) && !string.IsNullOrWhiteSpace(cachedJson))
            {
                _logger.LogDebug(">>> [GIS_CACHE_HIT]: Returned local immutable memory snapshot for {Key}", cacheKey);
                return cachedJson;
            }

            string filePath = Path.Combine(_cacheDirectory, snapshotFileName);

            try
            {
                // 2. Attempt HTTP Live API pull (Protected by Polly Circuit Breaker in DI)
                _logger.LogInformation(">>> [GIS_NETWORK_FETCH]: Querying {Url} via Polly Resilience Pipeline", externalUrl);
                var response = await _httpClient.GetAsync(externalUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var freshJson = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(freshJson))
                    {
                        // Save permanent In-Memory Cache (No Expiration / Immutable Fallback)
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetPriority(CacheItemPriority.NeverRemove);
                        _memoryCache.Set(cacheKey, freshJson, cacheOptions);

                        // Persist async snapshot to disk for offline cold boots
                        await File.WriteAllTextAsync(filePath, freshJson);
                        
                        _logger.LogInformation(">>> [GIS_SNAPSHOT_COMMITTED]: Synchronized {File} to local NVMe/Disk", snapshotFileName);
                        return freshJson;
                    }
                }
                
                _logger.LogWarning(">>> [GIS_API_FAULT]: Non-success status code {StatusCode} from {Url}", response.StatusCode, externalUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, ">>> [GIS_POLLY_INTERCEPT / NETWORK_OFFLINE]: Live API connection failed for {Url}. Initiating Fallback protocol.", externalUrl);
            }

            // 3. Fallback to offline local disk snapshot (Cold Boot without Internet)
            if (File.Exists(filePath))
            {
                _logger.LogWarning(">>> [GIS_OFFLINE_RECOVERY]: Loaded offline disk replica {File} due to network outage", snapshotFileName);
                var diskJson = await File.ReadAllTextAsync(filePath);
                _memoryCache.Set(cacheKey, diskJson, new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.NeverRemove));
                return diskJson;
            }

            // 4. Ultimate Emergency Mock (Prevents UI JS breaking during catastrophic cold init)
            _logger.LogError(">>> [GIS_EMERGENCY_FALLBACK]: No disk snapshot available for {Key}. Using embedded rescue JSON.", cacheKey);
            return emergencyMockJson;
        }
    }
}

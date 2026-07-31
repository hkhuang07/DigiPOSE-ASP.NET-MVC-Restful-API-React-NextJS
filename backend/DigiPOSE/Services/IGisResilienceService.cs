using System.Threading.Tasks;

namespace DigiPOSE.Services
{
    public interface IGisResilienceService
    {
        Task<string> GetProvincesAsync();
        Task<string> GetDistrictsByProvinceAsync(string provinceCode);
        Task<string> GetWardsByDistrictAsync(string districtCode);
    }
}

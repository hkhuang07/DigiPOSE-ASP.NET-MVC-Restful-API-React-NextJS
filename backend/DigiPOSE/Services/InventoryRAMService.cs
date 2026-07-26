using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using DigiPOSE.Models;

namespace DigiPOSE.Services
{
    public class InventoryRAMService : IInventoryRAMService
    {
        // Composite Key: (BranchId, ProductId) -> Live Stock Balance
        private readonly ConcurrentDictionary<(int BranchId, int ProductId), int> _stockMap = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public InventoryRAMService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public void InitializeOrUpdateStock(int branchId, int productId, int currentStock)
        {
            _stockMap[(branchId, productId)] = currentStock;
        }

        /// <summary>
        /// Retrieves stock balance from O(1) RAM cache. On cache miss, performs Lazy-Load query from SQL Server
        /// without blocking server startup or causing OOM spikes on large SKU catalogs.
        /// </summary>
        public async Task<int> GetStockAsync(int branchId, int productId)
        {
            var key = (branchId, productId);
            if (_stockMap.TryGetValue(key, out var cachedStock))
            {
                return cachedStock; // Fast Path: O(1) Zero I/O
            }

            return await LazyLoadFromDatabaseAsync(branchId, productId);
        }

        public async Task<Dictionary<int, int>> GetBulkStockAsync(int branchId, IEnumerable<int> productIds)
        {
            var results = new Dictionary<int, int>();
            foreach (var id in productIds.Distinct())
            {
                results[id] = await GetStockAsync(branchId, id);
            }
            return results;
        }

        public async Task<bool> TryDeductStockAsync(int branchId, int productId, int quantityToDeduct)
        {
            var key = (branchId, productId);

            // Ensure stock is cached in RAM before attempting Compare-And-Swap calculation
            if (!_stockMap.ContainsKey(key))
            {
                await LazyLoadFromDatabaseAsync(branchId, productId);
            }

            while (true)
            {
                if (!_stockMap.TryGetValue(key, out var currentStock))
                {
                    return false;
                }

                if (currentStock < quantityToDeduct)
                {
                    return false; // Fast-fail out of stock
                }

                int newStock = currentStock - quantityToDeduct;
                // Atomic Compare-And-Swap (CAS) loop prevents race conditions under extreme concurrent requests
                if (_stockMap.TryUpdate(key, newStock, currentStock))
                {
                    return true;
                }
            }
        }

        public void RestoreStock(int branchId, int productId, int quantity)
        {
            var key = (branchId, productId);
            _stockMap.AddOrUpdate(key, quantity, (_, current) => current + quantity);
        }

        private async Task<int> LazyLoadFromDatabaseAsync(int branchId, int productId)
        {
            var key = (branchId, productId);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DigiPoseDbContext>();

            var inv = await db.ProductInventories
                .AsNoTracking()
                .Where(pi => pi.BranchId == branchId && pi.ProductId == productId)
                .Select(pi => (int?)pi.StockQuantity)
                .FirstOrDefaultAsync();

            int realStock = inv ?? 0;
            _stockMap.TryAdd(key, realStock);
            return realStock;
        }
    }
}

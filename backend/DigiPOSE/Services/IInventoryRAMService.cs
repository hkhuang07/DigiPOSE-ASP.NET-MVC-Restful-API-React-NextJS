namespace DigiPOSE.Services
{
    public interface IInventoryRAMService
    {
        Task<bool> TryDeductStockAsync(int tenantId, int productId, int quantityToDeduct);
        void RestoreStock(int tenantId, int productId, int quantity);
        void InitializeOrUpdateStock(int tenantId, int productId, int currentStock);
        Task<int> GetStockAsync(int tenantId, int productId);
        Task<Dictionary<int, int>> GetBulkStockAsync(int tenantId, IEnumerable<int> productIds);
    }
}

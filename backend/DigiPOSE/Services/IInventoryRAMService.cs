namespace DigiPOSE.Services
{
    public interface IInventoryRAMService
    {
        Task<bool> TryDeductStockAsync(int branchId, int productId, int quantityToDeduct);
        void RestoreStock(int branchId, int productId, int quantity);
        void InitializeOrUpdateStock(int branchId, int productId, int currentStock);
        Task<int> GetStockAsync(int branchId, int productId);
        Task<Dictionary<int, int>> GetBulkStockAsync(int branchId, IEnumerable<int> productIds);
    }
}

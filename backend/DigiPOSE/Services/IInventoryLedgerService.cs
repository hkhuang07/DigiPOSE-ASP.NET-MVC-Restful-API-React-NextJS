using DigiPOSE.Models;

namespace DigiPOSE.Services
{
    public interface IInventoryLedgerService
    {
        /// <summary>
        /// Atomically mutates inventory balance, enforces EF row-versioning, emits an immutable audit ledger transaction,
        /// and synchronizes O(1) live RAM cache in real time.
        /// </summary>
        Task<(bool Success, string Message)> RecordTransactionAsync(
            int tenantId, 
            int productId, 
            int quantityDelta, 
            InventoryTxType txType, 
            int referenceOrderId = 0, 
            string? referenceDocumentNo = null,
            int? operatorUserId = null, 
            decimal unitCost = 0, 
            string? notes = null);

        /// <summary>
        /// Posts a completed StockVoucher (VoucherIn / VoucherOut / Adjustment), mutating warehouse balances and syncing RAM cache.
        /// Enforces Tenant Manager local tenant boundary checks.
        /// </summary>
        Task<(bool Success, string Message)> PostVoucherAsync(int voucherId, int approverUserId);

        /// <summary>
        /// Dispatches an inter-tenant stock transfer, deducting quantity from source tenant and setting state to InTransit.
        /// </summary>
        Task<(bool Success, string Message)> DispatchTransferAsync(int transferId, int dispatcherUserId);

        /// <summary>
        /// Receives an inter-tenant stock transfer at the target location, incrementing destination tenant stock and syncing RAM cache.
        /// </summary>
        Task<(bool Success, string Message)> ReceiveTransferAsync(int transferId, int receiverUserId);

        /// <summary>
        /// Reconciles physical shelf stock audit against software ledger and automatically emits balanced adjusting entries for any variances.
        /// </summary>
        Task<(bool Success, string Message)> PostStockAuditAsync(int auditId, int approverUserId);

        /// <summary>
        /// Exclusively restricted to Super Admin & Chief Accountant under force majeure ("Bất Khả Khang") anomalies.
        /// Enforces mandatory audit justification and emits an immutable EmergencyOverride ledger entry.
        /// </summary>
        Task<(bool Success, string Message)> ExecuteEmergencyOverrideAsync(int inventoryId, int newStockQuantity, int minStockLevel, int operatorUserId, string mandatoryReason);
    }
}

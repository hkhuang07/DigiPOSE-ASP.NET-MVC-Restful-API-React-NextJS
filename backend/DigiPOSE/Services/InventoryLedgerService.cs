using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigiPOSE.Models;
using System.Text.RegularExpressions;

namespace DigiPOSE.Services
{
    public class InventoryLedgerService : IInventoryLedgerService
    {
        private readonly DigiPoseDbContext _context;
        private readonly IInventoryRAMService _ramService;
        private readonly ILogger<InventoryLedgerService> _logger;

        public InventoryLedgerService(
            DigiPoseDbContext context, 
            IInventoryRAMService ramService, 
            ILogger<InventoryLedgerService> logger)
        {
            _context = context;
            _ramService = ramService;
            _logger = logger;
        }

        public async Task<(bool Success, string Message)> RecordTransactionAsync(
            int tenantId, 
            int productId, 
            int quantityDelta, 
            InventoryTxType txType, 
            int referenceOrderId = 0, 
            string? referenceDocumentNo = null,
            int? operatorUserId = null, 
            decimal unitCost = 0, 
            string? notes = null)
        {
            try
            {
                var inv = await _context.ProductInventories
                    .FirstOrDefaultAsync(i => i.TenantId == tenantId && i.ProductId == productId);

                if (inv == null)
                {
                    if (quantityDelta < 0 && txType != InventoryTxType.EmergencyOverride)
                    {
                        return (false, $"Cannot deduct stock: no inventory record exists for Product #{productId} at Tenant #{tenantId}.");
                    }

                    inv = new ProductInventory
                    {
                        TenantId = tenantId,
                        ProductId = productId,
                        StockQuantity = 0,
                        MinStockLevel = 5
                    };
                    _context.ProductInventories.Add(inv);
                    await _context.SaveChangesAsync();
                }

                int beforeQty = inv.StockQuantity;
                int afterQty = beforeQty + quantityDelta;

                // >>> [ENTERPRISE_LEDGER_MUTATOR]: Atomically mutate DB balance & emit immutable audit ledger
                inv.StockQuantity = afterQty;
                _context.ProductInventories.Update(inv);

                var tx = new InventoryTransaction
                {
                    TenantId = tenantId,
                    ProductId = productId,
                    QuantityDelta = quantityDelta,
                    BeforeQuantity = beforeQty,
                    AfterQuantity = afterQty,
                    UnitCost = unitCost,
                    OperatorUserId = operatorUserId,
                    Notes = notes ?? $"Auto-generated ledger entry for {txType}",
                    ReferenceDocumentNo = referenceDocumentNo ?? (referenceOrderId > 0 ? $"ORD-{referenceOrderId}" : "SYS-ADJ"),
                    TxType = txType,
                    ReferenceOrderId = referenceOrderId,
                    CreatedAt = DateTime.Now
                };

                _context.InventoryTransactions.Add(tx);
                await _context.SaveChangesAsync();

                // >>> [O(1) REAL-TIME RAM CACHE SYNC]: Prevent stale POS reads instantly without server reboot
                _ramService.InitializeOrUpdateStock(tenantId, productId, afterQty);

                _logger.LogInformation(">>> [LEDGER_RECORD_SUCCESS]: Tenant {TenantId} Product {ProductId} mutated from {Before} to {After} via {TxType}", tenantId, productId, beforeQty, afterQty, txType);
                return (true, "Transaction successfully recorded and RAM cache synchronized.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, ">>> [CONCURRENCY_EXCEPTION]: Row-version conflict while mutating inventory balance for Product {ProductId}", productId);
                return (false, "Optimistic concurrency lock conflict. Please reload and try again.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ">>> [LEDGER_ERROR]: Failed to record inventory transaction");
                return (false, $"Ledger failure: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> PostVoucherAsync(int voucherId, int approverUserId)
        {
            var voucher = await _context.StockVouchers
                .Include(v => v.StockVoucherDetails)
                .FirstOrDefaultAsync(v => v.VoucherId == voucherId);

            if (voucher == null)
                return (false, "Voucher document not found.");
            
            if (voucher.Status == VoucherStatus.Posted)
                return (false, "Voucher has already been posted to the general stock ledger.");

            if (voucher.StockVoucherDetails == null || !voucher.StockVoucherDetails.Any())
                return (false, "Voucher contains no item details to post.");

            foreach (var detail in voucher.StockVoucherDetails)
            {
                int delta = detail.Quantity;
                InventoryTxType txType = InventoryTxType.VoucherIn;

                string typeUpper = voucher.VoucherType.ToUpperInvariant();
                if (typeUpper.Contains("OUT") || typeUpper.Contains("XUAT") || typeUpper.Contains("EXPORT") || typeUpper.Contains("RETURN") || typeUpper.Contains("TRẢ"))
                {
                    delta = -Math.Abs(detail.Quantity);
                    txType = InventoryTxType.VoucherOut;
                }
                else if (typeUpper.Contains("ADJUST") || typeUpper.Contains("ĐIỀU CHỈNH"))
                {
                    txType = InventoryTxType.Adjustment;
                }

                var res = await RecordTransactionAsync(
                    voucher.TenantId,
                    detail.ProductId,
                    delta,
                    txType,
                    0,
                    !string.IsNullOrEmpty(voucher.VoucherCode) ? voucher.VoucherCode : $"POV-{voucher.VoucherId}",
                    approverUserId,
                    detail.ActualPrice,
                    $"Voucher posted: {voucher.VoucherType} by User #{approverUserId}");

                if (!res.Success)
                    return (false, $"Failed processing item #{detail.ProductId}: {res.Message}");
            }

            voucher.Status = VoucherStatus.Posted;
            voucher.ApprovedByUserId = approverUserId;
            voucher.ApprovedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return (true, "Voucher successfully posted to inventory ledger and O(1) RAM cache synchronized.");
        }

        public async Task<(bool Success, string Message)> DispatchTransferAsync(int transferId, int dispatcherUserId)
        {
            var transfer = await _context.StockTransfers
                .Include(t => t.StockTransferDetails)
                .FirstOrDefaultAsync(t => t.TransferId == transferId);

            if (transfer == null)
                return (false, "Transfer record not found.");

            if (transfer.Status != StockTransferStatus.Draft)
                return (false, "Transfer is not in Draft status.");

            if (transfer.StockTransferDetails == null || !transfer.StockTransferDetails.Any())
                return (false, "Transfer contains no items.");

            foreach (var detail in transfer.StockTransferDetails)
            {
                var res = await RecordTransactionAsync(
                    transfer.SourceTenantId,
                    detail.ProductId,
                    -Math.Abs(detail.Quantity),
                    InventoryTxType.TransferOut,
                    0,
                    transfer.TransferCode,
                    dispatcherUserId,
                    detail.UnitCost,
                    $"Inter-tenant dispatch to Tenant #{transfer.DestinationTenantId}");

                if (!res.Success)
                    return (false, $"Failed deducting item #{detail.ProductId} at source: {res.Message}");
            }

            transfer.Status = StockTransferStatus.InTransit;
            transfer.DispatchedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return (true, "Transfer dispatched successfully. Source warehouse inventory deducted.");
        }

        public async Task<(bool Success, string Message)> ReceiveTransferAsync(int transferId, int receiverUserId)
        {
            var transfer = await _context.StockTransfers
                .Include(t => t.StockTransferDetails)
                .FirstOrDefaultAsync(t => t.TransferId == transferId);

            if (transfer == null)
                return (false, "Transfer record not found.");

            if (transfer.Status != StockTransferStatus.InTransit)
                return (false, "Transfer is not currently in transit.");

            foreach (var detail in transfer.StockTransferDetails!)
            {
                int qtyToAdd = detail.ReceivedQuantity > 0 ? detail.ReceivedQuantity : detail.Quantity;
                var res = await RecordTransactionAsync(
                    transfer.DestinationTenantId,
                    detail.ProductId,
                    Math.Abs(qtyToAdd),
                    InventoryTxType.TransferIn,
                    0,
                    transfer.TransferCode,
                    receiverUserId,
                    detail.UnitCost,
                    $"Inter-tenant receipt from Tenant #{transfer.SourceTenantId}");

                if (!res.Success)
                    return (false, $"Failed incrementing item #{detail.ProductId} at destination: {res.Message}");
            }

            transfer.Status = StockTransferStatus.Completed;
            transfer.ApproverUserId = receiverUserId;
            transfer.ReceivedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return (true, "Transfer received successfully. Destination warehouse balance and RAM cache synchronized.");
        }

        public async Task<(bool Success, string Message)> PostStockAuditAsync(int auditId, int approverUserId)
        {
            var audit = await _context.StockAudits
                .Include(a => a.StockAuditDetails)
                .FirstOrDefaultAsync(a => a.AuditId == auditId);

            if (audit == null)
                return (false, "Stock audit record not found.");

            if (audit.Status == StockAuditStatus.ReconciledAndPosted)
                return (false, "Stock audit has already been reconciled and posted.");

            if (audit.StockAuditDetails == null || !audit.StockAuditDetails.Any())
                return (false, "Audit contains no variance items.");

            foreach (var detail in audit.StockAuditDetails)
            {
                if (detail.VarianceQuantity == 0)
                    continue; // Zero variance, physical count matches system

                var res = await RecordTransactionAsync(
                    audit.TenantId,
                    detail.ProductId,
                    detail.VarianceQuantity,
                    InventoryTxType.StockAudit,
                    0,
                    !string.IsNullOrEmpty(audit.AuditCode) ? audit.AuditCode : $"AUD-{audit.AuditId}",
                    approverUserId,
                    detail.UnitCost,
                    $"Stock count reconciliation: {detail.Reason ?? "Variance adjustment"}");

                if (!res.Success)
                    return (false, $"Failed reconciling item #{detail.ProductId}: {res.Message}");
            }

            audit.Status = StockAuditStatus.ReconciledAndPosted;
            audit.ApproverUserId = approverUserId;
            await _context.SaveChangesAsync();

            return (true, "Stock audit reconciled successfully. All physical variances adjusted in DB and RAM cache.");
        }

        public async Task<(bool Success, string Message)> ExecuteEmergencyOverrideAsync(
            int inventoryId, 
            int newStockQuantity, 
            int minStockLevel, 
            int operatorUserId, 
            string mandatoryReason)
        {
            if (string.IsNullOrWhiteSpace(mandatoryReason) || mandatoryReason.Trim().Length < 15)
            {
                return (false, ">>> [SAFETY_VIOLATION]: Force Majeure emergency override requires a detailed written audit justification of at least 15 characters.");
            }

            var inv = await _context.ProductInventories.FindAsync(inventoryId);
            if (inv == null)
            {
                return (false, "Inventory record not found in database.");
            }

            if (newStockQuantity == inv.StockQuantity)
            {
                // Only MinStockLevel changed, no accounting mutation needed
                inv.MinStockLevel = minStockLevel;
                _context.ProductInventories.Update(inv);
                await _context.SaveChangesAsync();
                return (true, "Minimum stock threshold updated without ledger balance mutation.");
            }

            int delta = newStockQuantity - inv.StockQuantity;
            inv.MinStockLevel = minStockLevel;
            _context.ProductInventories.Update(inv);

            var res = await RecordTransactionAsync(
                inv.TenantId,
                inv.ProductId,
                delta,
                InventoryTxType.EmergencyOverride,
                0,
                $"EMRG-{inv.InventoryId}-{DateTime.Now:yyyyMMddHHmm}",
                operatorUserId,
                0,
                $">>> [FORCE_MAJEURE_AUDIT // BẤT KHẢ KHÁNG]: {mandatoryReason.Trim()}");

            if (!res.Success)
            {
                return res;
            }

            return (true, $"Emergency Override confirmed. Stock balance coerced to {newStockQuantity} in DB and O(1) RAM cache with audit trail EMRG-{inv.InventoryId}.");
        }
    }
}

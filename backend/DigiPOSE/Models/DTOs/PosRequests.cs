using System.ComponentModel.DataAnnotations;

namespace DigiPOSE.Models.DTOs
{
    public class CreateDraftRequest
    {
        [Required]
        public int BranchId { get; set; }
        
        [Required]
        public int ShiftId { get; set; }
        
        [Required]
        public int UserId { get; set; }
    }

    public class AddItemRequest
    {
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
        
        // >>> [EDGE_SAFETY]: Chống add lặp mặt hàng nếu máy quét barcode bị nảy liên tục trong 50ms
        [Required]
        public Guid ClientScanId { get; set; } = Guid.NewGuid();
    }

    public class RemoveItemRequest
    {
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
    }

    public class CheckoutRequest
    {
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public int PaymentMethodId { get; set; }
        
        public int? CustomerId { get; set; }

        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Tendered amount must be non-negative")]
        public decimal TenderedAmount { get; set; } = 0;

        // >>> [CRITICAL_IDEMPOTENCY]: Khóa duy nhất từ máy thu ngân.
        // Nạp đi nạp lại 100 lần vẫn chỉ tạo đúng 1 Đơn, không bao giờ chia đôi hóa đơn!
        [Required]
        public Guid IdempotencyKey { get; set; }
    }

    // >>> [HIGH_EFFECT_UI_DTO]: Response gửi trả khi Checkout xong, giúp POS UI nảy số tồn kho lập tức!
    public record CheckoutResponseDto(
        int OrderId,
        string InvoiceNumber,
        DateTime ProcessedAt,
        bool IsReplay, // True nếu đây là phản hồi lặp lại do Retry (đã chốt trước đó)
        Dictionary<int, int> LiveStockBalances,
        decimal TenderedAmount = 0,
        decimal ChangeAmount = 0
    );
}

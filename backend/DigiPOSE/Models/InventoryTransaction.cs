using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public enum InventoryTxType
    {
        PosSale = 1,
        Restock = 2,
        Adjustment = 3,
        Return = 4,
        WebSale = 5,
        VoucherIn = 6,
        VoucherOut = 7,
        TransferOut = 8,
        TransferIn = 9,
        StockAudit = 10,
        EmergencyOverride = 11 // Mandatory justification & Super Admin RBAC enforcement
    }

    public class InventoryTransaction
    {
        [Key]
        [Display(Name = "Transaction ID")]
        public int TransactionId { get; set; }

        [Required]
        [Display(Name = "Product ID")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Branch ID")]
        public int BranchId { get; set; }

        [Required]
        [Display(Name = "Quantity Delta")]
        public int QuantityDelta { get; set; } // Negative for sale deduction, positive for addition

        [Display(Name = "Before Quantity")]
        public int BeforeQuantity { get; set; }

        [Display(Name = "After Quantity")]
        public int AfterQuantity { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Unit Cost")]
        public decimal UnitCost { get; set; } = 0;

        [Display(Name = "Operator User ID")]
        public int? OperatorUserId { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes / Justification")]
        public string? Notes { get; set; }

        [StringLength(100)]
        [Display(Name = "Reference Document No")]
        public string? ReferenceDocumentNo { get; set; }

        [Required]
        [Display(Name = "Transaction Type")]
        public InventoryTxType TxType { get; set; }

        [Required]
        [Display(Name = "Reference Order ID")]
        public int ReferenceOrderId { get; set; }

        [Required]
        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Product? Product { get; set; }
        public Branch? Branch { get; set; }
        [ForeignKey("OperatorUserId")]
        public User? OperatorUser { get; set; }
    }
}

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
        WebSale = 5 // Dedicated ledger code for Online Storefront E-Commerce fulfillment
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
    }
}

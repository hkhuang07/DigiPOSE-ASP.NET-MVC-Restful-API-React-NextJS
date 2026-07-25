using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class ProductInventory
    {
        [Key] public int InventoryId { get; set; }
        [Display(Name = "Branch")]
        [Required(ErrorMessage = "Please select a branch.")]
        public int BranchId { get; set; }
        
        [Display(Name = "Product")]
        [Required(ErrorMessage = "Please select a product.")]
        public int ProductId { get; set; }
        
        [Display(Name = "Stock Quantity")]
        [Required(ErrorMessage = "Stock Quantity is required.")]
        public int StockQuantity { get; set; } = 0;
        
        [Display(Name = "Min Stock Level")]
        [Required(ErrorMessage = "Min Stock Level is required.")]
        public int MinStockLevel { get; set; } = 0;

        [Timestamp] public byte[]? RowVersion { get; set; }

        public Branch? Branch { get; set; }
        public Product? Product { get; set; }
    }
}
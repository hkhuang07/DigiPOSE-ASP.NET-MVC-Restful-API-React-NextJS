using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class StockTransferDetail
    {
        [Key]
        public int TransferDetailId { get; set; }

        [Required]
        public int TransferId { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Dispatched Quantity")]
        public int Quantity { get; set; }

        [Display(Name = "Received Quantity")]
        public int ReceivedQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Unit Cost")]
        public decimal UnitCost { get; set; } = 0;

        [ForeignKey("TransferId")]
        public StockTransfer? StockTransfer { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}

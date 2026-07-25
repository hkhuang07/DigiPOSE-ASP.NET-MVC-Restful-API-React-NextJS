using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class StockVoucherDetail
    {
        [Key] public int VoucherDetailId { get; set; }
        [Display(Name = "Voucher ID")]
        [Required(ErrorMessage = "Please select a voucher.")]
        public int VoucherId { get; set; }
        
        [Display(Name = "Product")]
        [Required(ErrorMessage = "Please select a product.")]
        public int ProductId { get; set; }
        
        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "Quantity is required.")]
        public int Quantity { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Actual Price")]
        public decimal ActualPrice { get; set; }

        public StockVoucher? StockVoucher { get; set; }
        public Product? Product { get; set; }
    }
}

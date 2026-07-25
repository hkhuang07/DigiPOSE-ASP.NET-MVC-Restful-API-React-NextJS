using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class OrderDetail
    {
        [Key] public int OrderDetailId { get; set; }
        [Display(Name = "Order ID")]
        [Required(ErrorMessage = "Please select an Order.")]
        public int OrderId { get; set; }
        
        [Display(Name = "Product")]
        [Required(ErrorMessage = "Please select a Product.")]
        public int ProductId { get; set; }
        
        [Display(Name = "Nature")]
        [Required(ErrorMessage = "Please select a Nature.")]
        public int NatureId { get; set; }
        
        [Display(Name = "Tax Type")]
        [Required(ErrorMessage = "Please select a Tax Type.")]
        public int TaxTypeId { get; set; }

        [Display(Name = "Quantity")]
        [Required(ErrorMessage = "Quantity is required.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Product Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "Product Name cannot exceed 100 characters.")]
        [Display(Name = "Product Name")] 
        public string ProductName { get; set; } = null!;
        
        [Required(ErrorMessage = "Unit Name cannot be empty.")]
        [StringLength(50, ErrorMessage = "Unit Name cannot exceed 50 characters.")]
        [Display(Name = "Unit Name")] 
        public string UnitName { get; set; } = null!;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Unit Price")] 
        public decimal UnitPrice { get; set; } 
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Discount Rate")] 
        public decimal DiscountRate { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Discount Amount")] 
        public decimal DiscountAmount { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Tax Rate")] 
        public decimal TaxRate { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Tax Amount")] 
        public decimal TaxAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Total Amount")] 
        public decimal TotalAmount { get; set; } = 0; // = (Qty*UnitPrice) - Discount + Tax

        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        [Display(Name = "Notes")] 
        public string? Notes { get; set; }
        
        [Display(Name = "Is Free")] 
        public bool IsFree { get; set; } = false; 

        public Order? Order { get; set; }
        public Product? Product { get; set; }
        public ItemNature? ItemNature { get; set; }
        public TaxType? TaxType { get; set; }
    }
}
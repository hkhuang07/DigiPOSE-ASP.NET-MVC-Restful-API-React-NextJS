using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Order
    {
        [Key]
        [Display(Name = "Order ID")] 
        public int OrderId { get; set; }

        [Display(Name = "Idempotency Key")]
        [Required]
        public Guid IdempotencyKey { get; set; } = Guid.NewGuid();

        [StringLength(50)]
        [Display(Name = "Invoice Number")]
        public string? InvoiceNumber { get; set; }
        
        [Display(Name = "Tenant")] 
        [Required(ErrorMessage = "Please select a tenant.")]
        public int TenantId { get; set; }
        
        [Display(Name = "Shift")] 
        [Required(ErrorMessage = "Please select a shift.")]
        public int ShiftId { get; set; }
        
        [Display(Name = "Employee")] 
        [Required(ErrorMessage = "Please select an employee.")]
        public int UserId { get; set; }
        
        [Display(Name = "Customer")] 
        public int? CustomerId { get; set; }

        [StringLength(100, ErrorMessage = "Snapshot Customer Name cannot exceed 100 characters.")]
        [Display(Name = "Customer Name (Snapshot)")] 
        public string? SnapshotCustomerName { get; set; }
        
        [Column(TypeName = "varchar(20)")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        [Display(Name = "Customer Phone (Snapshot)")] 
        public string? SnapshotCustomerPhone { get; set; }
        
        [Display(Name = "Status")] 
        [Required(ErrorMessage = "Please select an order status.")]
        public int StatusId { get; set; }
        
        [Display(Name = "Payment Method")] 
        public int? PaymentMethodId { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Gross Amount")] 
        public decimal GrossAmount { get; set; } 
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Discount")] 
        public decimal DiscountAmount { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Tax")] 
        public decimal TaxAmount { get; set; } = 0;
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Total Amount")] 
        public decimal TotalAmount { get; set; }

        [StringLength(255, ErrorMessage = "Discount Reason cannot exceed 255 characters.")]
        [Display(Name = "Discount Reason")] 
        public string? DiscountReason { get; set; }
        
        [Display(Name = "Created Date")] 
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // >>> [ENTERPRISE_POS_SETTLEMENT]: Cashier terminal tender and change balancing attributes
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Tendered Amount")] 
        public decimal TenderedAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Change Amount")] 
        public decimal ChangeAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "VAT Rounding Difference")] 
        public decimal VatRoundingDifference { get; set; } = 0;

        // >>> [ECOMMERCE_LOGISTICS_INTEGRATION]: Online Storefront shipping & customer delivery metadata
        [StringLength(255)]
        [Display(Name = "Shipping Address")]
        public string? ShippingAddress { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Shipping Fee")]
        public decimal ShippingFee { get; set; } = 0;

        [StringLength(1000)]
        [Display(Name = "Order Notes")]
        public string? OrderNotes { get; set; }

        public Shift? Shift { get; set; }
        public User? User { get; set; }
        public Customer? Customer { get; set; }
        [ForeignKey("StatusId")]
        public OrderStatus? OrderStatus { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public ICollection<OrderDetail>? OrderDetails { get; set; }

        public Invoice? invoice { get; set; }
    }
}
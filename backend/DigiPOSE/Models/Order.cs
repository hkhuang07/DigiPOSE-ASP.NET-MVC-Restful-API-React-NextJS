using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Order
    {
        [Key]
        [Display(Name = "Order ID")] 
        public int OrderId { get; set; }
        
        [Display(Name = "Branch")] 
        [Required(ErrorMessage = "Please select a branch.")]
        public int BranchId { get; set; }
        
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

        public Shift? Shift { get; set; }
        public User? User { get; set; }
        public Customer? Customer { get; set; }
        public OrderStatus? OrderStatus { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public ICollection<OrderDetail>? OrderDetails { get; set; }

        public Invoice? invoice { get; set; }
    }
}
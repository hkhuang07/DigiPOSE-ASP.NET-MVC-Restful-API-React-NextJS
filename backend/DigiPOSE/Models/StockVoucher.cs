using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public enum VoucherStatus
    {
        Draft = 1,
        PendingApproval = 2,
        Posted = 3,
        Cancelled = 4
    }

    public class StockVoucher
    {
        [Key] public int VoucherId { get; set; }

        [StringLength(50)]
        [Display(Name = "Voucher Code")]
        public string VoucherCode { get; set; } = string.Empty;

        [Display(Name = "Tenant")] 
        [Required(ErrorMessage = "Please select a tenant.")]
        public int TenantId { get; set; }
        
        [Display(Name = "Employee")] 
        [Required(ErrorMessage = "Please select an employee.")]
        public int UserId { get; set; }
        
        [Display(Name = "Supplier")] 
        public int? SupplierId { get; set; }
        
        [Required(ErrorMessage = "Voucher Type cannot be empty.")]
        [StringLength(50, ErrorMessage = "Voucher Type cannot exceed 50 characters.")]
        [Display(Name = "Voucher Type")] 
        public string VoucherType { get; set; } = null!;
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Total Value")] 
        public decimal TotalValue { get; set; }

        [Display(Name = "Voucher Status")]
        public VoucherStatus Status { get; set; } = VoucherStatus.Draft;

        [Display(Name = "Approved By")]
        public int? ApprovedByUserId { get; set; }

        [Display(Name = "Approved At")]
        public DateTime? ApprovedAt { get; set; }
        
        [Display(Name = "Created At")] 
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Tenant? Tenant { get; set; }
        [ForeignKey("UserId")]
        public User? User { get; set; }
        [ForeignKey("ApprovedByUserId")]
        public User? ApprovedByUser { get; set; }
        public Supplier? Supplier { get; set; }
        public ICollection<StockVoucherDetail>? StockVoucherDetails { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class StockVoucher
    {
        [Key] public int VoucherId { get; set; }
        [Display(Name = "Branch")] 
        [Required(ErrorMessage = "Please select a branch.")]
        public int BranchId { get; set; }
        
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
        
        [Display(Name = "Created At")] 
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Branch? Branch { get; set; }
        public User? User { get; set; }
        public Supplier? Supplier { get; set; }
        public ICollection<StockVoucherDetail>? StockVoucherDetails { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class StockAuditDetail
    {
        [Key]
        public int AuditDetailId { get; set; }

        [Required]
        public int AuditId { get; set; }

        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "System Recorded Quantity")]
        public int SystemQuantity { get; set; }

        [Required]
        [Display(Name = "Physical Counted Quantity")]
        public int PhysicalQuantity { get; set; }

        [Display(Name = "Variance Quantity")]
        public int VarianceQuantity { get; set; } // PhysicalQuantity - SystemQuantity

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Unit Cost Valuation")]
        public decimal UnitCost { get; set; } = 0;

        [StringLength(200)]
        [Display(Name = "Variance Reason")]
        public string? Reason { get; set; }

        [ForeignKey("AuditId")]
        public StockAudit? StockAudit { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public enum StockAuditStatus
    {
        Draft = 1,
        UnderReview = 2,
        ReconciledAndPosted = 3,
        Cancelled = 4
    }

    public class StockAudit
    {
        [Key]
        public int AuditId { get; set; }

        [StringLength(50)]
        [Display(Name = "Audit Code")]
        public string AuditCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Branch")]
        public int BranchId { get; set; }

        [Required]
        [Display(Name = "Audit Date")]
        public DateTime AuditDate { get; set; } = DateTime.Today;

        [Display(Name = "Status")]
        public StockAuditStatus Status { get; set; } = StockAuditStatus.Draft;

        [Required]
        [Display(Name = "Auditor")]
        public int AuditorUserId { get; set; }

        [Display(Name = "Reconciliation Approver")]
        public int? ApproverUserId { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes / General Summary")]
        public string? Notes { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("BranchId")]
        public Branch? Branch { get; set; }

        [ForeignKey("AuditorUserId")]
        public User? AuditorUser { get; set; }

        [ForeignKey("ApproverUserId")]
        public User? ApproverUser { get; set; }

        public ICollection<StockAuditDetail>? StockAuditDetails { get; set; }
    }
}

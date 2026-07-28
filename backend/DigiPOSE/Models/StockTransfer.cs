using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public enum StockTransferStatus
    {
        Draft = 1,
        InTransit = 2,
        Completed = 3,
        Cancelled = 4
    }

    public class StockTransfer
    {
        [Key]
        public int TransferId { get; set; }

        [StringLength(50)]
        [Display(Name = "Transfer Code")]
        public string TransferCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Source Branch")]
        public int SourceBranchId { get; set; }

        [Required]
        [Display(Name = "Destination Branch")]
        public int DestinationBranchId { get; set; }

        [Required]
        [Display(Name = "Initiator")]
        public int InitiatorUserId { get; set; }

        [Display(Name = "Approver / Receiver")]
        public int? ApproverUserId { get; set; }

        [Display(Name = "Status")]
        public StockTransferStatus Status { get; set; } = StockTransferStatus.Draft;

        [Display(Name = "Dispatched At")]
        public DateTime? DispatchedAt { get; set; }

        [Display(Name = "Received At")]
        public DateTime? ReceivedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [ForeignKey("SourceBranchId")]
        public Branch? SourceBranch { get; set; }

        [ForeignKey("DestinationBranchId")]
        public Branch? DestinationBranch { get; set; }

        [ForeignKey("InitiatorUserId")]
        public User? InitiatorUser { get; set; }

        [ForeignKey("ApproverUserId")]
        public User? ApproverUser { get; set; }

        public ICollection<StockTransferDetail>? StockTransferDetails { get; set; }
    }
}

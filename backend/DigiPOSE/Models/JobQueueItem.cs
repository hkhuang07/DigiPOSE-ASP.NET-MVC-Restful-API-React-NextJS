using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class JobQueueItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string JobType { get; set; } = string.Empty;

        [Required]
        public string PayloadJson { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed

        public int RetryCount { get; set; } = 0;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? ProcessedAt { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    /// <summary>
    /// Enterprise 3NF mapping table authorizing explicit multi-tenant terminal ownership for operational personnel.
    /// Replaces legacy single-tenant fallback routing with strict Zero-Trust access control.
    /// </summary>
    public class UserTenant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserTenantId { get; set; }

        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Required]
        public int TenantId { get; set; }
        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime AssignedAt { get; set; } = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}

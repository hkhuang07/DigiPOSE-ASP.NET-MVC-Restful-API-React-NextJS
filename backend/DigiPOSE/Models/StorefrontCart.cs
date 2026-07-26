using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    /// <summary>
    /// Phase 6.2 Dedicated Online Storefront E-Commerce Shopping Cart.
    /// Strictly isolates transient online guest carts from accounting order logs (Orders table),
    /// eliminating the Abandoned Cart Pollution Trap.
    /// </summary>
    public class StorefrontCart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartId { get; set; }

        [Required]
        public Guid CartGuid { get; set; } = Guid.NewGuid();

        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        [MaxLength(255)]
        public string CustomerIdentity { get; set; } = "Guest Shopper";

        [MaxLength(255)]
        public string? SnapshotCustomerName { get; set; }

        [MaxLength(50)]
        public string? SnapshotCustomerPhone { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal GrossAmount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Indexed for automated background cleanup of stale abandoned online carts (> 30 days)
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<StorefrontCartItem> Items { get; set; } = new List<StorefrontCartItem>();
    }

    /// <summary>
    /// Represents an item inside an active online E-Commerce storefront cart session.
    /// </summary>
    public class StorefrontCartItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartItemId { get; set; }

        [Required]
        public int CartId { get; set; }
        [ForeignKey("CartId")]
        public virtual StorefrontCart? Cart { get; set; }

        [Required]
        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product? Product { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        // Snapshot price at the exact moment of adding to online cart
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [MaxLength(50)]
        public string UnitName { get; set; } = "Unit";
    }
}

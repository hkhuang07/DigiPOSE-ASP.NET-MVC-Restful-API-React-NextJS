using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Subscription
    {
        [Key]
        [Display(Name = "Subscription ID")]
        public int SubscriptionId { get; set; }

        [Display(Name = "Customer")]
        [Required(ErrorMessage = "Please select a customer.")]
        public int CustomerId { get; set; }

        [Display(Name = "Product / SaaS Package")]
        [Required(ErrorMessage = "Please select a product package.")]
        public int ProductId { get; set; }

        [Display(Name = "Order Reference")]
        [Required(ErrorMessage = "Please link to the original order.")]
        public int OrderId { get; set; }

        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [StringLength(500, ErrorMessage = "License Key cannot exceed 500 characters.")]
        [Display(Name = "License Key / Token")]
        public string? LicenseKey { get; set; }

        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters.")]
        [Display(Name = "Status")]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE, EXPIRED, CANCELED

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Display(Name = "Updated Date")]
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public Customer? Customer { get; set; }
        public Product? Product { get; set; }
        public Order? Order { get; set; }
    }
}

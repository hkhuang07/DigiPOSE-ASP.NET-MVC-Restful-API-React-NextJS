using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    // >>> [ENTERPRISE DOMAIN ENTITY]: Production POS Retail Document & Corporate B2B Accounting Voucher
    // Architectural benchmarked against industry standard POS schemas (biz_retail / RetailEntity).
    // Decouples trade documentation and tax records from basic sales orders to satisfy strict fiscal audit compliance.
    public class Retail
    {
        [Key]
        [Display(Name = "Retail Document ID")]
        public int RetailId { get; set; }

        [Required]
        [Display(Name = "Associated Order ID")]
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Document No cannot be empty.")]
        [StringLength(50)]
        [Display(Name = "Document No (doc_no)")]
        public string DocNo { get; set; } = null!; // Unique accounting document serial (e.g., BL-20260727-XXXX)

        [StringLength(50)]
        [Display(Name = "Retail Receipt No")]
        public string? RetailNo { get; set; } // Human-readable receipt code / counter transaction serial

        // >>> [DOCUMENT CLASSIFICATION]: "POS_RETAIL" (Standard B2C Walk-in) vs "B2B_INVOICE" (Corporate Tax Bill)
        [Required]
        [StringLength(30)]
        [Display(Name = "Document Type")]
        public string DocType { get; set; } = "POS_RETAIL";

        // >>> [MULTI-TENANT & DEVICE SCOPING]: Exact location, warehouse, station & cashier accountability
        [Required]
        [Display(Name = "Tenant ID")]
        public int TenantId { get; set; }

        [Display(Name = "Warehouse ID")]
        public int? WarehouseId { get; set; }

        [Display(Name = "Counter ID")]
        public int? CounterId { get; set; }

        [Required]
        [Display(Name = "Shift ID")]
        public int ShiftId { get; set; }

        [Required]
        [Display(Name = "Cashier Operator ID")]
        public int UserId { get; set; }

        // >>> [CLIENT & FISCAL AUTHENTICATION]: Complete B2B corporate billing & e-Invoice credentials
        [Display(Name = "Customer / VIP ID")]
        public int? CustomerId { get; set; }

        [StringLength(255)]
        [Display(Name = "Buyer Legal Name / Corporate Title")]
        public string? BuyerLegalName { get; set; }

        [StringLength(50)]
        [Display(Name = "Corporate Tax Code (MST)")]
        public string? BuyerTaxCode { get; set; }

        [StringLength(500)]
        [Display(Name = "Buyer Billing Address")]
        public string? BuyerAddress { get; set; }

        [StringLength(100)]
        [Display(Name = "E-Invoice Email Address")]
        public string? BuyerEmail { get; set; }

        [Display(Name = "Payment Method ID")]
        public int? PaymentMethodId { get; set; }

        // >>> [FINANCIAL SETTLEMENT & TAX BALANCING]: Comprehensive fiscal metrics per POS standard
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Total Quantity")]
        public decimal TotalQuantity { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Gross Subtotal Amount")]
        public decimal GrossAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Document Discount Rate")]
        public decimal DocDiscountRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "VIP Discount Rate")]
        public decimal VipDiscountRate { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Total Discount Amount")]
        public decimal DiscountAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "VAT Tax Amount")]
        public decimal VatAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Net Amount Before Settlement")]
        public decimal NetAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Total Payable Amount")]
        public decimal TotalAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Tendered Received Amount")]
        public decimal TenderedAmount { get; set; } = 0;

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Change Amount Returned")]
        public decimal ChangeAmount { get; set; } = 0;

        [Display(Name = "Print Count")]
        public int PrintNo { get; set; } = 1;

        [Display(Name = "Transaction Open Date")]
        public DateTime Date { get; set; } = DateTime.Now;

        [Display(Name = "Settlement Completed Date")]
        public DateTime EndDate { get; set; } = DateTime.Now;

        // >>> [CRITICAL_IDEMPOTENCY]: Aligned with terminal idempotency key to prevent duplicated billing records
        [Required]
        [Display(Name = "Idempotency Key")]
        public Guid IdempotencyKey { get; set; }

        [Display(Name = "E-Invoice Reported")]
        public bool IsEInvoiceReported { get; set; } = false;

        [StringLength(255)]
        [Display(Name = "Digital Security Verification Hash")]
        public string? DigitalSignatureHash { get; set; }

        [StringLength(500)]
        [Display(Name = "Operational Notes / Remarks")]
        public string? Notes { get; set; }

        // Navigation Properties
        public Order? Order { get; set; }
        public Tenant? Tenant { get; set; }
        public Counter? Counter { get; set; }
        public Shift? Shift { get; set; }
        public User? User { get; set; }
        public Customer? Customer { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
    }
}

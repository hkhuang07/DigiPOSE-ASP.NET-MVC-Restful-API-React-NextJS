using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Invoice
    {
        [Key] public int InvoiceId { get; set; } 
        [Display(Name = "Order ID")]
        [Required(ErrorMessage = "Please select an Order.")]
        public int OrderId { get; set; } 

        [Display(Name = "Invoice Status")] 
        [Required(ErrorMessage = "Please select an Invoice Status.")]
        public int InvoiceStatusId { get; set; }
        
        [Display(Name = "Invoice Type")] 
        [Required(ErrorMessage = "Please select an Invoice Type.")]
        public int InvoiceTypeId { get; set; }

        [Required(ErrorMessage = "Form cannot be empty.")]
        [StringLength(20, ErrorMessage = "Form cannot exceed 20 characters.")]
        [Display(Name = "Form")] 
        public string Form { get; set; } = null!;   // Mẫu số
        
        [Required(ErrorMessage = "Series cannot be empty.")]
        [StringLength(20, ErrorMessage = "Series cannot exceed 20 characters.")]
        [Display(Name = "Series")] 
        public string Series { get; set; } = null!; // Ký hiệu
        
        [Required(ErrorMessage = "Invoice No cannot be empty.")]
        [StringLength(50, ErrorMessage = "Invoice No cannot exceed 50 characters.")]
        [Display(Name = "Invoice No")] 
        public string InvoiceNo { get; set; } = null!; // Số hóa đơn

        [Display(Name = "Invoice Date")] 
        public DateTime Date { get; set; } = DateTime.Now;
        
        [Display(Name = "Signed Date")] 
        public DateTime? SignedDate { get; set; }

        [Column(TypeName = "decimal(18,4)")] 
        [Display(Name = "Exchange Rate")] 
        public decimal? ExchangeRate { get; set; }
        
        [StringLength(10, ErrorMessage = "Currency Code cannot exceed 10 characters.")] 
        [Display(Name = "Currency Code")] 
        public string? CurrencyCode { get; set; } 

        [StringLength(50, ErrorMessage = "Original Invoice No cannot exceed 50 characters.")] 
        [Display(Name = "Original Invoice No")] 
        public string? OriginalInvoiceNo { get; set; }
        
        [StringLength(255, ErrorMessage = "Origin Invoice Reason cannot exceed 255 characters.")] 
        [Display(Name = "Origin Invoice Reason")] 
        public string? OriginInvoiceReason { get; set; }

        [StringLength(50, ErrorMessage = "Tax Authority Code cannot exceed 50 characters.")] 
        [Display(Name = "Tax Authority Code")] 
        public string? TaxAuthorityCode { get; set; }
        
        [StringLength(100, ErrorMessage = "Invoice Lookup Code cannot exceed 100 characters.")] 
        [Display(Name = "Invoice Lookup Code")] 
        public string? InvoiceLookupCode { get; set; }

        public Order Order { get; set; } = null!;
        public InvoiceStatus? InvoiceStatus { get; set; }
        public InvoiceType? InvoiceType { get; set; }
    }
}
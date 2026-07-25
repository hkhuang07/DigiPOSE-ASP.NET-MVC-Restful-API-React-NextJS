using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace DigiPOSE.Models
{
    public class Supplier
    {
        [Key] public int SupplierId { get; set; }
        [Required(ErrorMessage = "Supplier Name cannot be empty.")]
        [StringLength(150, ErrorMessage = "Supplier Name cannot exceed 150 characters.")]
        [Display(Name = "Supplier Name")] 
        public string SupplierName { get; set; } = null!;
        
        [Column(TypeName = "varchar(20)")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        [Display(Name = "Phone")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone numbers must start with 0 and consist of 10-11 digits.")]
        public string? Phone { get; set; }
        
        [StringLength(100, ErrorMessage = "Contact Person cannot exceed 100 characters.")]
        [Display(Name = "Contact Person")] 
        public string? ContactPerson { get; set; }
        
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Invalid Email Format.")]
        [Display(Name = "Email Address")] 
        public string? Email { get; set; }
        
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        [Display(Name = "Address")] 
        public string? Address { get; set; }
        
        [Column(TypeName = "varchar(20)")]
        [StringLength(20, ErrorMessage = "Tax Code cannot exceed 20 characters.")]
        [Display(Name = "Tax Code")] 
        public string? TaxCode { get; set; }
        
        [StringLength(150, ErrorMessage = "Bank Name cannot exceed 150 characters.")]
        [Display(Name = "Bank Name")] 
        public string? BankName { get; set; }
        
        [Column(TypeName = "varchar(50)")]
        [StringLength(50, ErrorMessage = "Bank Account cannot exceed 50 characters.")]
        [Display(Name = "Bank Account")] 
        public string? BankAccount { get; set; }
        
        [StringLength(150, ErrorMessage = "Website cannot exceed 150 characters.")]
        [Display(Name = "Website")] 
        public string? Website { get; set; }
        
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Description")] 
        public string? Description { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = false)]
        [Display(Name = "Debt Balance (VND)")] 
        public decimal DebtBalance { get; set; } = 0;

        [StringLength(255)][Display(Name = "Image URL")] public string? ImageUrl { get; set; }
        [NotMapped][Display(Name = "Upload Logo")] public IFormFile? ImageUpload { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<StockVoucher>? StockVouchers { get; set; }
    }
}

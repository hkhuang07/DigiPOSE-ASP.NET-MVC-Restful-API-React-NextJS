using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace DigiPOSE.Models
{
    public class Customer
    {
        [Key] 
        public int CustomerId { get; set; }

        [Display(Name = "Customer Type")]
        [Required(ErrorMessage = "Select customer type, please.")]
        public int CustomeTypeId { get; set; }

        [Required(ErrorMessage = "Full name cannot be blank.")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        [Display(Name = "Full name")]       
        public string FullName { get; set; } = null!;

        [StringLength(200, ErrorMessage = "Company Name cannot exceed 200 characters.")]
        [Display(Name = "Company Name")] 
        public string? CompanyName { get; set; }

        [Column(TypeName = "varchar(20)")]
        [StringLength(20, ErrorMessage = "Tax Code cannot exceed 20 characters.")]
        [Display(Name = "Tax Code")] 
        public string? TaxCode { get; set; }

        [Column(TypeName = "varchar(50)")]
        [StringLength(50, ErrorMessage = "Budget Code cannot exceed 50 characters.")]
        [Display(Name = "Budget Code")] 
        public string? BudgetCode { get; set; }
 
        [Column(TypeName = "varchar(20)")]
        [StringLength(20, ErrorMessage = "Id No cannot exceed 20 characters.")]
        [Display(Name = "Id No")] 
        public string? IdNo { get; set; }
 
        [Column(TypeName = "varchar(20)")]
        [StringLength(20, ErrorMessage = "Contact phone number cannot exceed 20 characters.")]
        [Display(Name = "Contact phonenumber ")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone numbers must start with 0 and consist of 10-11 digits.")]          
        public string? PhoneNumber { get; set; }
 
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Invalid Email Format.")]
        [Display(Name = "Email Address")]
        public string? Email { get; set; }
 
        [Display(Name = "Delivery Address")]        
        public string? Address { get; set; }
 
        [StringLength(150, ErrorMessage = "Bank Name cannot exceed 150 characters.")]
        [Display(Name = "Bank Name")] 
        public string? BankName { get; set; }
 
        [Column(TypeName = "varchar(50)")]
        [StringLength(50, ErrorMessage = "Bank Account cannot exceed 50 characters.")]
        [Display(Name = "Bank Account")] 
        public string? BankAccount { get; set; }
 
        [Display(Name = "Reward Points")] 
        public int RewardPoints { get; set; } = 0;
 
 
        [Column(TypeName = "decimal(18,4)")] 
        [Display(Name = "Debt balance (VND)")] 
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = false)]
        public decimal DebtBalance { get; set; } = 0;
 
        [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
        [Display(Name = "Notes")] 
        public string? Notes { get; set; }
 
        [StringLength(255)]
        [Display(Name = "Image path")]
        public string? ImageUrl { get; set; }

        [NotMapped]
        [Display(Name = "Upload profile picture")]
        public IFormFile? ImageUpload { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        public CustomeType? CustomeType { get; set; }
    }
}

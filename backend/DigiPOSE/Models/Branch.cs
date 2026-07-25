using Microsoft.EntityFrameworkCore.Infrastructure;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 
using Microsoft.AspNetCore.Http;

namespace DigiPOSE.Models
{
    public class Branch
    {
        [Key] public int BranchId { get; set; }
        [Required(ErrorMessage = "Branch Name cannot be empty.")]
        [StringLength(150, ErrorMessage = "Branch Name cannot exceed 150 characters.")]
        [Display(Name = "Branch Name")] 
        public string BranchName { get; set; } = null!;
        
        [StringLength(200, ErrorMessage = "Slug cannot exceed 200 characters.")] 
        public string? Slug { get; set; }
        
        [StringLength(255, ErrorMessage = "Address cannot exceed 255 characters.")]
        [Display(Name = "Address")] 
        public string? Address { get; set; }
        
        [StringLength(50, ErrorMessage = "Contact Phone cannot exceed 50 characters.")]
        [Display(Name = "Contact Phone")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone numbers must start with 0 and consist of 10-11 digits.")]
        public string? ContactPhone { get; set; }
        
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Invalid Email Format.")]
        [Display(Name = "Contact Email")] 
        public string? Email { get; set; }
        
        [StringLength(100, ErrorMessage = "Manager Name cannot exceed 100 characters.")]
        [Display(Name = "Manager Name")] 
        public string? ManagerName { get; set; }
        
        [StringLength(255, ErrorMessage = "Notes cannot exceed 255 characters.")]
        [Display(Name = "Notes")] 
        public string? Notes { get; set; }

        [StringLength(255)][Display(Name = "Image URL")] public string? ImageUrl { get; set; }
        [NotMapped][Display(Name = "Upload Image")] public IFormFile? ImageUpload { get; set; }

        public bool IsActive { get; set; } = true;
        public ICollection<User>? Users { get; set; }
        public ICollection<Counter>? Counters { get; set; }
        public ICollection<StockVoucher>? StockVourchers { get; set; }
        public ICollection<ProductInventory>? ProductInventories { get; set; }
    }
}
    
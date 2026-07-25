using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace DigiPOSE.Models
{
    public class Manufacturer
    {
        [Key] public int ManufacturerId { get; set; }
        [Required(ErrorMessage = "Manufacturer Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "Manufacturer Name cannot exceed 100 characters.")]
        [Display(Name = "Manufacturer Name")] 
        public string ManufacturerName { get; set; } = null!;
        
        [StringLength(200, ErrorMessage = "Slug cannot exceed 200 characters.")] 
        public string? Slug { get; set; }
        
        [StringLength(255, ErrorMessage = "Logo URL cannot exceed 255 characters.")]
        [Display(Name = "Logo URL")] 
        public string? ImageUrl { get; set; }
        
        [NotMapped]
        [Display(Name = "Upload Logo")] 
        public IFormFile? ImageUpload { get; set; }

        [StringLength(100, ErrorMessage = "Contact Person cannot exceed 100 characters.")]
        [Display(Name = "Contact Person")] 
        public string? ContactPerson { get; set; }
        
        [Column(TypeName = "varchar(20)")]
        [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters.")]
        [Display(Name = "Phone")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone numbers must start with 0 and consist of 10-11 digits.")]
        public string? Phone { get; set; }
        
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
        
        [StringLength(150, ErrorMessage = "Website cannot exceed 150 characters.")]
        [Display(Name = "Website")] 
        public string? Website { get; set; }
        
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Description")] 
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Product>? Products { get; set; }
    }
}

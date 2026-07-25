using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace DigiPOSE.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Category Name is required.")]
        [StringLength(100, ErrorMessage = "Category Name cannot exceed 100 characters.")]
        [Display(Name = "Category Name")]
        public string CategoryName { get; set; } = null!;

        [StringLength(150, ErrorMessage = "Slug cannot exceed 150 characters.")]
        public string? Slug { get; set; }

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(255, ErrorMessage = "Image URL cannot exceed 255 characters.")]
        [Display(Name = "Image URL")] 
        public string? ImageUrl { get; set; }
        
        [NotMapped]
        [Display(Name = "Upload Image")] 
        public IFormFile? ImageUpload { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<Product>? Products { get; set; }
    }
}

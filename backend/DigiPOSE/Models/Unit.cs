using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DigiPOSE.Models
{
    public class Unit
    {
        [Key] public int UnitId { get; set; }
        [Required(ErrorMessage = "Unit Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "Unit Name cannot exceed 100 characters.")]
        [Display(Name = "Unit Name")] 
        public string UnitName { get; set; } = null!; 
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")] 
        public string? Description { get; set; }

        public ICollection<Product>? Products { get; set; }
    }
}

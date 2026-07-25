using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DigiPOSE.Models
{
    public class CustomeType
    {
        [Key] public int CustomeTypeId { get; set; }
        [Required(ErrorMessage = "Customer Type cannot be empty.")]
        [StringLength(100, ErrorMessage = "Customer Type cannot exceed 100 characters.")]
        [Display(Name = "Customer Type")] 
        public string TypeName { get; set; } = null!;
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")] 
        [Display(Name = "Description")]  
        public string? Description { get; set; }
    }
}

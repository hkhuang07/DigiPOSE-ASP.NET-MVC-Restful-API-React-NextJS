using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class ShiftStatus
    {
        [Key] public int StatusId { get; set; }
        [Required(ErrorMessage = "Status Name cannot be empty.")]
        [StringLength(50, ErrorMessage = "Status Name cannot exceed 50 characters.")]
        [Display(Name = "Shift Status Name")] 
        public string StatusName { get; set; } = null!;
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")] 
        public string? Description { get; set; }
        public ICollection<Shift>? Shifts { get; set; }
    }
}
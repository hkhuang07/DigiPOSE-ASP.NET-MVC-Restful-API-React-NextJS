using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Counter
    {
        [Key] public int CounterId { get; set; }
        [Display(Name = "Branch")] 
        [Required(ErrorMessage = "Please select a branch.")]
        public int BranchId { get; set; }
        
        [Required(ErrorMessage = "Counter Name cannot be empty.")]
        [StringLength(50, ErrorMessage = "Counter Name cannot exceed 50 characters.")]
        [Display(Name = "Counter Name")] 
        public string CounterName { get; set; } = null!;
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public Branch? Branch { get; set; }

        public ICollection<Shift>? Shifts { get; set; }

    }
}

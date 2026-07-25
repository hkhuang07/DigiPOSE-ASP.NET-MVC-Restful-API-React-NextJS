using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Shift
    {
        [Key] public int ShiftId { get; set; }
        [Display(Name = "Employee")] 
        [Required(ErrorMessage = "Please select an employee.")]
        public int UserId { get; set; }
        
        [Display(Name = "Counter")] 
        [Required(ErrorMessage = "Please select a counter.")]
        public int CounterId { get; set; }
        
        [Display(Name = "Status")] 
        [Required(ErrorMessage = "Please select a status.")]
        public int StatusId { get; set; } 
        
        [Display(Name = "Start time")] 
        [Required(ErrorMessage = "Start Time is required.")]
        public DateTime StartTime { get; set; }
        
        [Display(Name = "End time")] 
        public DateTime? EndTime { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Start Cash")] 
        public decimal StartCash { get; set; }
        
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "End Cash")] 
        public decimal? EndCash { get; set; }

        public User? User { get; set; }
        public Counter? Counter { get; set; }
        public ShiftStatus? Status { get; set; }
    }
}

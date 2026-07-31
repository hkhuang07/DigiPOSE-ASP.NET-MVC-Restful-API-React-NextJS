using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Counter
    {
        [Key] public int CounterId { get; set; }
        [Display(Name = "Tenant")] 
        [Required(ErrorMessage = "Please select a tenant.")]
        public int TenantId { get; set; }
        
        [Required(ErrorMessage = "Counter Name cannot be empty.")]
        [StringLength(50, ErrorMessage = "Counter Name cannot exceed 50 characters.")]
        [Display(Name = "Counter Name")] 
        public string CounterName { get; set; } = null!;
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Tenant? Tenant { get; set; }

        public ICollection<Shift>? Shifts { get; set; }

    }
}

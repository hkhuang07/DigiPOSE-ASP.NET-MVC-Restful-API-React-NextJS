using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace DigiPOSE.Models
{
    public class PaymentMethod
    {
        [Key] public int PaymentMethodId { get; set; }
        [Required(ErrorMessage = "Method Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "Method Name cannot exceed 100 characters.")]
        [Display(Name = "Method Name")] 
        public string MethodName { get; set; } = null!;
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")] 
        public string? Description { get; set; }

        public ICollection<Order>? Orders { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class InvoiceStatus
    {
        [Key] public int InvoiceStatusId { get; set; }
        [Required(ErrorMessage = "Status Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "Status Name cannot exceed 100 characters.")]
        [Display(Name = "Status Name")] 
        public string StatusName { get; set; } = null!;
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")] 
        [Display(Name = "Description")] 
        public string? Description { get; set; }

        public ICollection<Invoice>? Invoices { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class InvoiceType
    {
        [Key] public int InvoiceTypeId { get; set; }
        [Required(ErrorMessage = "Type Name cannot be empty.")]
        [StringLength(255, ErrorMessage = "Type Name cannot exceed 255 characters.")]
        [Display(Name = "Type Name")] 
        public string TypeName { get; set; } = null!;
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")] 
        public string? Description { get; set; }

        public ICollection<Invoice>? Invoices { get; set; }
    }
}
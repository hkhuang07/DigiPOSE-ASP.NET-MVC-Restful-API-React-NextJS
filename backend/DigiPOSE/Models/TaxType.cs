using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class TaxType
    {
        [Key] public int TaxTypeId { get; set; }

        [Required(ErrorMessage = "Tax Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "Tax Name cannot exceed 100 characters.")]
        [Display(Name = "Tax Name")]
        public string TaxName { get; set; } = null!; // VD: "Thuế GTGT 8%", "Không chịu thuế"

        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "Tax Percentage")]
        public decimal TaxPercentage { get; set; }

        [Required(ErrorMessage = "Tax Xml Code cannot be empty.")]
        [StringLength(20, ErrorMessage = "Tax Xml Code cannot exceed 20 characters.")]
        [Display(Name = "Tax Xml Code")]
        public string TaxXmlCode { get; set; } = null!;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        public ICollection<Product>? Products { get; set; }
        public ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}
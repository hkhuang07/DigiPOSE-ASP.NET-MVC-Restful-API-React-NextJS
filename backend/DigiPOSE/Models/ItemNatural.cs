using System.ComponentModel.DataAnnotations;

namespace DigiPOSE.Models
{
    //Định nghĩa Tính chất dòng bán (Ví dụ: 1. Bán thường, 2. Khuyến mãi, 3. Chiết khấu)
    public class ItemNature
    {
        [Key] public int NatureId { get; set; }

        [Required(ErrorMessage = "Nature Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "Nature Name cannot exceed 100 characters.")]
        [Display(Name = "Nature Name")]
        public string NatureName { get; set; } = null!;

        // Mã dùng để đẩy lên API của Cơ quan Thuế (1, 2, 3, 4 theo Thông tư 78)
        [StringLength(50, ErrorMessage = "Tax Xml Code cannot exceed 50 characters.")]
        [Display(Name = "Tax Xml Code")]
        public string? TaxXmlCode { get; set; }

        [Display(Name = "Status")]
        public bool IsActive { get; set; } = true;

        public ICollection<OrderDetail>? OrderDetails { get; set; }
    }
}
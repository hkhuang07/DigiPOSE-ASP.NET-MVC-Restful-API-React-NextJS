using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class SystemModule
    {
        [Key]
        public int ModuleId { get; set; }

        [Required(ErrorMessage = "Module Name cannot be empty.")]
        [StringLength(100, ErrorMessage = "Module Name cannot exceed 100 characters.")]
        [Display(Name = "Module Name")]
        public string ModuleName { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Icon class cannot exceed 50 characters.")]
        [Display(Name = "Icon")]
        public string? Icon { get; set; }

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        [Display(Name = "Active Status")]
        public bool IsActive { get; set; } = true;

        public ICollection<Permission>? Permissions { get; set; }
    }
}

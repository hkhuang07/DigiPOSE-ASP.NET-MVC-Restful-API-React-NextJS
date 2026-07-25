using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }

        [Required(ErrorMessage = "Permission Name cannot be empty.")]
        [StringLength(150, ErrorMessage = "Permission Name cannot exceed 150 characters.")]
        [Display(Name = "Permission Name")]
        public string PermissionName { get; set; } = null!;

        [Display(Name = "System Module")]
        public int? SystemModuleId { get; set; }

        [ForeignKey("SystemModuleId")]
        public SystemModule? Module { get; set; }

        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        public ICollection<PermissionRole>? PermissionRoles { get; set; }
    }
}

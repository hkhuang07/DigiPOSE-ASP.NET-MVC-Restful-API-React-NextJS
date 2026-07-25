using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Role
    {
        [Key] public int RoleId { get; set; }
        [Required(ErrorMessage = "Role Name cannot be empty.")]
        [StringLength(150, ErrorMessage = "Role Name cannot exceed 150 characters.")]
        [Display(Name = "Role Name")] 
        public string RoleName { get; set; } = null!;
        
        [StringLength(255, ErrorMessage = "Description cannot exceed 255 characters.")]
        [Display(Name = "Description")] 
        public string? Description { get; set; }
        public ICollection<User>? Users { get; set; }
        public ICollection<PermissionRole>? PermissionRoles { get; set; }
    }
}

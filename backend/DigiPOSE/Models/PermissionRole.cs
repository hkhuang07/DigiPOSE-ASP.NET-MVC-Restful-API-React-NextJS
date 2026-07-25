using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class PermissionRole
    {
        [Display(Name = "Role")]
        public int RoleId { get; set; }
        
        [ForeignKey("RoleId")]
        public Role? Role { get; set; }

        [Display(Name = "Permission")]
        public int PermissionId { get; set; }
        
        [ForeignKey("PermissionId")]
        public Permission? Permission { get; set; }
    }
}

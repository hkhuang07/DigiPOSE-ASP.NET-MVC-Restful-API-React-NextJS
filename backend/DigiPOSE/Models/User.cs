using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; 
using Microsoft.AspNetCore.Http;

namespace DigiPOSE.Models
{
    public class User
    {
        [Key] 
        public int UserId { get; set; }
        
        [Display(Name = "Authorities")] 
        [Required(ErrorMessage = "Please select a role.")]
        public int RoleId { get; set; }
        
        [Display(Name = "Branch")] 
        [Required(ErrorMessage = "Please select a branch.")]
        public int BranchId { get; set; }
        
        [Required(ErrorMessage = "Username cannot be blank.")]
        [StringLength(50, MinimumLength = 4, ErrorMessage = "Username must be between 4 and 50 characters.")]
        [Display(Name = "Username")]
        public string UserName { get; set; } = null!;
        
        [Required(ErrorMessage = "Password cannot be blank.")]
        [StringLength(255)]
        [DataType(DataType.Password)]
        [Display(Name = "Encrypted password")]
        public string PasswordHash { get; set; } = null!;
                
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }
        
        [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
        [EmailAddress(ErrorMessage = "Email is not in the correct format.")]
        [Display(Name = "Contact Email")]        
        public string? Email { get; set; }
                
        [Column(TypeName = "varchar(20)")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [Display(Name = "Contact Phone")]
        [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Phone numbers must start with 0 and consist of 10-11 digits.")]           
        public string? PhoneNumber { get; set; }
        
        [StringLength(255)][Display(Name = "Avatar URL")] public string? ImageUrl { get; set; }
        [NotMapped][Display(Name = "Upload Avatar")] public IFormFile? ImageUpload { get; set; }

        [Display(Name = "Is Active")] 
        public bool IsActive { get; set; } = true;

        public Role? Role { get; set; }
        public Branch? Branch { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DigiPOSE.Models.DTOs
{
    public class CreateDraftRequest
    {
        [Required]
        public int BranchId { get; set; }
        
        [Required]
        public int ShiftId { get; set; }
        
        [Required]
        public int UserId { get; set; }
    }

    public class AddItemRequest
    {
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }
    }

    public class RemoveItemRequest
    {
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public int ProductId { get; set; }
    }

    public class CheckoutRequest
    {
        [Required]
        public int OrderId { get; set; }
        
        [Required]
        public int PaymentMethodId { get; set; }
        
        public int? CustomerId { get; set; }
    }
}

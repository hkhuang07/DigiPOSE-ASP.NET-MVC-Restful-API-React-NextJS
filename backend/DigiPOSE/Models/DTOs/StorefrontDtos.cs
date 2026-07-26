using System.ComponentModel.DataAnnotations;

namespace DigiPOSE.Models.DTOs
{
    // ==========================================
    // STOREFRONT CART & LINE ITEM REQUEST DTOs
    // ==========================================
    public class CartItemRequest
    {
        [Required]
        public int CartId { get; set; } // Can map to OrderId (Status=4/5) or Cart Session ID
        
        [Required]
        public int ProductId { get; set; }
        
        [Required]
        [Range(1, 9999, ErrorMessage = "Quantity must be between 1 and 9999.")]
        public int Quantity { get; set; } = 1;
    }

    public class UpdateQuantityRequest
    {
        [Required]
        public int CartId { get; set; }
        [Required]
        public int ProductId { get; set; }
        [Required]
        public int NewQuantity { get; set; }
    }

    public class RemoveCartItemRequest
    {
        [Required]
        public int CartId { get; set; }
        [Required]
        public int ProductId { get; set; }
    }

    public class StorefrontCheckoutRequest
    {
        [Required]
        public int CartId { get; set; }
        [Required]
        public int PaymentMethodId { get; set; }
        public int? CustomerId { get; set; }
        public string? ShippingAddress { get; set; }
        public string? ContactPhone { get; set; }
        public string? CustomerNotes { get; set; }

        // Mandatory client-generated UUID to prevent double-billing on network retries
        [Required]
        public Guid IdempotencyKey { get; set; } = Guid.NewGuid();
    }

    // ==========================================
    // CATALOG SEARCH & SEO FILTERING DTOs
    // ==========================================
    public class CatalogSearchFilter
    {
        public string? Query { get; set; }
        public int? CategoryId { get; set; }
        public int? ProductTypeId { get; set; }
        public int? ManufacturerId { get; set; }
        public int? ItemNatureId { get; set; } // 1 = Physical Retail, 2 = Digital SaaS Subscription
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStockOnly { get; set; } = false;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string SortBy { get; set; } = "name_asc"; // name_asc, price_asc, price_desc, newest
    }

    public class SeoProductResponse
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public string? ImageUrl { get; set; }
        public string? Slug { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? ManufacturerName { get; set; }
        public string ProductTypeName { get; set; } = null!;
        public bool IsDigitalSaaS { get; set; }
        
        // SEO Meta Fields for Next.JS Server-Side Rendering & Indexing
        public string MetaTitle { get; set; } = null!;
        public string MetaDescription { get; set; } = null!;
        public string MetaKeywords { get; set; } = null!;
        public string OpenGraphImage { get; set; } = null!;
    }

    public class CartSummaryResponse
    {
        public int CartId { get; set; }
        public Guid CartGuid { get; set; }
        public string CustomerIdentity { get; set; } = "Guest Shopper";
        public string CartState { get; set; } = "CardEmpty"; // CardEmpty vs Card (Active Cart)
        public int TotalQuantity { get; set; }
        public decimal GrossPrice { get; set; }
        public decimal TotalTaxAmount { get; set; }
        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalPrice { get; set; }
        public List<CartDetailItem> Items { get; set; } = new List<CartDetailItem>();
    }

    public class CartDetailItem
    {
        public int ProductId { get; set; }
        public string SKU { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string UnitName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal LineTax { get; set; }
        public string? ImageUrl { get; set; }
    }
}

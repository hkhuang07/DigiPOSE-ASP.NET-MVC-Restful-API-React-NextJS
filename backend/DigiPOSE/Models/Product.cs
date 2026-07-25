using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class Product
    {
        [Key] 
        public int ProductId { get; set; }
        
        [Display(Name = "Product Category")]
        [Required(ErrorMessage = "Please select category.")]
        public int CategoryId { get; set; }
        
        [Display(Name = "Product Type")] 
        [Required(ErrorMessage = "Please select product type.")]
        public int ProductTypeId { get; set; }

        [Display(Name = "Item Nature")]
        [Required(ErrorMessage = "Please select item nature (Physical/Digital).")]
        public int ItemNatureId { get; set; }
        
        [Display(Name = "Unit of Measurement")]
        [Required(ErrorMessage = "Please select unit of measurement.")]
        public int UnitId { get; set; }
        
        [Display(Name = "Manufacturer")] 
        public int? ManufacturerId { get; set; }
        
        [Display(Name = "Tax Type")] 
        public int TaxTypeId { get; set; }


        [Required(ErrorMessage = "SKU code cannot be empty.")]
        [StringLength(50)]
        [Display(Name = "SKU Code / Barcode")]
        public string SKU { get; set; } = null!;

        [Column(TypeName = "varchar(50)")]
        [StringLength(50)]
        [Display(Name = "Barcode")] 
        public string? Barcode { get; set; }

        [Required(ErrorMessage = "Product Name cannot be empty.")]
        [StringLength(150, ErrorMessage = "Product Name cannot be 150 characters.")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = null!;


        [Required(ErrorMessage = "Selling price cannot be blank.")]
        [Range(0, 9999999999, ErrorMessage = "Selling price must be greater than or equal to 0.")]
        [Display(Name = "Base selling price (VND)")]
        [Column(TypeName = "decimal(18,4)")]
        public decimal BasePrice { get; set; }

        [Range(0, 9999999999, ErrorMessage = "Cost price must be greater than or equal to 0.")]
        [Display(Name = "Cost price (VND)")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = false)]
        [Column(TypeName = "decimal(18,4)")]        
        public decimal CostPrice { get; set; } = 0;

        //Save image path to Database
        [StringLength(255)]
        [Display(Name = "Image path")]
        public string? ImageUrl { get; set; }

        [NotMapped]
        [Display(Name = "Upload profile picture")]
        public IFormFile? ImageUpload { get; set; }


        [StringLength(200)]
        [Display(Name = "Slug")] 
        public string? Slug { get; set; }

        [StringLength(1000)]
        [Display(Name = "Detailed description")]
        [DataType(DataType.MultilineText)]        
        public string? Description { get; set; }

        //[Display(Name = "Min Stock Level")] 
        //public int MinStockLevel { get; set; } = 0;
        //[Display(Name = "Max Stock Level")] 
        //public int MaxStockLevel { get; set; } = 1000;

        [Display(Name = "Business status")]
        public bool IsActive { get; set; } = true;

        [Timestamp] public byte[]? RowVersion { get; set; }


        public TaxType? TaxType { get; set; }
        public Category? Category { get; set; }
        public Unit? Unit { get; set; }
        public Manufacturer? Manufacturer { get; set; }
        public ProductType? ProductType { get; set; }
        public ItemNature? ItemNature { get; set; }

        public ICollection<ProductInventory>? ProductInventories { get; set; }
        public ICollection<StockVoucherDetail>? StockVoucherDetails { get; set; }
        public ICollection<OrderDetail>? OrderDetails { get; set; }

    }
}
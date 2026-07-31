using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DigiPOSE.Models
{
    public class CustomerAddress
    {
        [Key]
        public int AddressId { get; set; }

        [Required]
        [ForeignKey("Customer")]
        public int CustomerId { get; set; }

        [Required]
        [StringLength(20)]
        public string ProvinceCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ProvinceName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string DistrictCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string DistrictName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string WardCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string WardName { get; set; } = string.Empty;

        [StringLength(250)]
        public string? StreetAddress { get; set; }

        [Column(TypeName = "decimal(12,8)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(12,8)")]
        public decimal? Longitude { get; set; }

        public bool IsDefault { get; set; } = false;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual Customer? Customer { get; set; }
    }
}

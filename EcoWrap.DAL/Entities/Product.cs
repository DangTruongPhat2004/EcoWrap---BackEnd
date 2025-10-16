using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities
{
    [Table("Product")]
    public partial class Product
    {
        [Key]
        [Column("Product_id")]
        [StringLength(15)]
        [Unicode(false)]
        public string ProductId { get; set; } = null!;

        [Column("Seller_id")]
        public int SellerId { get; set; }

        [Column("Category_id")]
        public int CategoryId { get; set; }

        [Column("Material_type")]
        [StringLength(50)]
        public string? MaterialType { get; set; }

        [Column("Product_name")]
        [StringLength(100)]
        public string ProductName { get; set; } = null!;

        [Column("Product_description")]
        [StringLength(255)]
        public string? ProductDescription { get; set; }

        [Column("Unit")]
        [StringLength(50)]
        public string? Unit { get; set; }

        [Column("Size")]
        [StringLength(50)]
        public string? Size { get; set; }

        [Column("Ink_type")]
        [StringLength(50)]
        public string? InkType { get; set; }

        [Column("Print_process")]
        [StringLength(50)]
        public string? PrintProcess { get; set; }

        [Column("Product_price", TypeName = "decimal(18,2)")]
        public decimal ProductPrice { get; set; }

        [Column("Product_status")]
        [StringLength(20)]
        public string? ProductStatus { get; set; } = "Out stock";

        [Column("Image_file")]
        public string? ImageFile { get; set; }

        [Column("Certificate_file")]
        public string? CertificateFile { get; set; }

        [Column("Design_file")]
        public string? DesignFile { get; set; }

        [Column("Created_date", TypeName = "datetime")]
        public DateTime? CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("SellerId")]
        [InverseProperty("Products")]
        public virtual Seller Seller { get; set; } = null!;

        [ForeignKey("CategoryId")]
        [InverseProperty("Products")]
        public virtual Category Category { get; set; } = null!;

        [InverseProperty("Product")]
        public virtual ICollection<CarbonFootprint> CarbonFootprints { get; set; } = new List<CarbonFootprint>();

        [InverseProperty("Product")]
        public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

        [InverseProperty("Product")]
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
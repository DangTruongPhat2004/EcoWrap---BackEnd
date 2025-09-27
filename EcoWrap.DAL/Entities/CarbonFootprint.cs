using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("CarbonFootprint")]
public partial class CarbonFootprint
{
    [Key]
    [Column("Carbon_id")]
    public int CarbonId { get; set; }

    [Column("Product_id")]
    [StringLength(15)]
    [Unicode(false)]
    public string ProductId { get; set; } = null!;

    [Column("Warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("Product_weight", TypeName = "decimal(8, 2)")]
    public decimal? ProductWeight { get; set; }

    [Column("Disposal_method")]
    [StringLength(20)]
    public string DisposalMethod { get; set; } = null!;

    [Column("Carbon_value", TypeName = "decimal(6, 3)")]
    public decimal? CarbonValue { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("CarbonFootprints")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("WarehouseId")]
    [InverseProperty("CarbonFootprints")]
    public virtual Warehouse Warehouse { get; set; } = null!;
}

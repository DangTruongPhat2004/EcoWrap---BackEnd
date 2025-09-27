using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Warehouse")]
public partial class Warehouse
{
    [Key]
    [Column("Warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("Seller_id")]
    public int SellerId { get; set; }

    [StringLength(255)]
    public string? Address { get; set; }

    [InverseProperty("Warehouse")]
    public virtual ICollection<CarbonFootprint> CarbonFootprints { get; set; } = new List<CarbonFootprint>();

    [InverseProperty("Warehouse")]
    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    [ForeignKey("SellerId")]
    [InverseProperty("Warehouses")]
    public virtual Seller Seller { get; set; } = null!;

    [InverseProperty("Warehouse")]
    public virtual ICollection<Shipping> Shippings { get; set; } = new List<Shipping>();
}

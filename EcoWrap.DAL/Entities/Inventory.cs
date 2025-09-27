using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Inventory")]
public partial class Inventory
{
    [Key]
    [Column("Inventory_id")]
    public int InventoryId { get; set; }

    [Column("Warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("Product_id")]
    [StringLength(15)]
    [Unicode(false)]
    public string ProductId { get; set; } = null!;

    [Column("Stock_quantity")]
    public int? StockQuantity { get; set; }

    [Column("Last_updated", TypeName = "datetime")]
    public DateTime? LastUpdated { get; set; }

    [ForeignKey("ProductId")]
    [InverseProperty("Inventories")]
    public virtual Product Product { get; set; } = null!;

    [ForeignKey("WarehouseId")]
    [InverseProperty("Inventories")]
    public virtual Warehouse Warehouse { get; set; } = null!;
}

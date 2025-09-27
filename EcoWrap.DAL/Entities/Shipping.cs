using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Shipping")]
public partial class Shipping
{
    [Key]
    [Column("Shipping_id")]
    public int ShippingId { get; set; }

    [Column("Order_id")]
    [StringLength(20)]
    [Unicode(false)]
    public string OrderId { get; set; } = null!;

    [Column("Customer_id")]
    public int CustomerId { get; set; }

    [Column("Warehouse_id")]
    public int WarehouseId { get; set; }

    [Column("Shipping_address")]
    [StringLength(255)]
    public string? ShippingAddress { get; set; }

    [Column("Shipping_status")]
    [StringLength(20)]
    public string ShippingStatus { get; set; } = null!;

    [Column("Created_date", TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column("Updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Shippings")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("OrderId")]
    [InverseProperty("Shippings")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("WarehouseId")]
    [InverseProperty("Shippings")]
    public virtual Warehouse Warehouse { get; set; } = null!;
}

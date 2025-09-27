using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("OrderItem")]
public partial class OrderItem
{
    [Key]
    [Column("Order_item_id")]
    [StringLength(10)]
    [Unicode(false)]
    public string OrderItemId { get; set; } = null!;

    [Column("Order_id")]
    [StringLength(20)]
    [Unicode(false)]
    public string OrderId { get; set; } = null!;

    [Column("Product_id")]
    [StringLength(15)]
    [Unicode(false)]
    public string ProductId { get; set; } = null!;

    [Column("Unit_price", TypeName = "decimal(18, 2)")]
    public decimal? UnitPrice { get; set; }

    public int? Quantity { get; set; }

    public int? Subtotal { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("OrderItems")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("OrderItems")]
    public virtual Product Product { get; set; } = null!;
}

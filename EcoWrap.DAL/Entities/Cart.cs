using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Cart")]
public partial class Cart
{
    [Key]
    [Column("Cart_id")]
    [StringLength(10)]
    [Unicode(false)]
    public string CartId { get; set; } = null!;

    [Column("Product_id")]
    [StringLength(15)]
    [Unicode(false)]
    public string ProductId { get; set; } = null!;

    [Column("Customer_id")]
    public int CustomerId { get; set; }

    [Column("Total_price", TypeName = "decimal(18, 2)")]
    public decimal? TotalPrice { get; set; }

    public int? Quantity { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Carts")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("Carts")]
    public virtual Product Product { get; set; } = null!;
}

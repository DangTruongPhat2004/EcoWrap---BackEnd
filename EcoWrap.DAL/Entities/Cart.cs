using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EcoWrap.DAL.Entities;

[Table("Cart")]
public partial class Cart
{
    [Key]
    [Column("Cart_id")]
    [StringLength(10)]
    [Unicode(false)]
    public int CartId { get; set; }

    [Column("Customer_id")]
    public int CustomerId { get; set; }

    [Column("Cart_total_price")]
    public decimal? CartTotalPrice { get; set; }

    [Column("Created_date", TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column("IsActive")]
    public bool? IsActive { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [ForeignKey("CustomerId")]
    [InverseProperty("Carts")]
    public virtual Customer Customer { get; set; } = null!;

    //public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
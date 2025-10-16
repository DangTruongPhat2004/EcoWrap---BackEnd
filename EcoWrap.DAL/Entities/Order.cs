using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EcoWrap.DAL.Entities;

public partial class Order
{
    [Key]
    [Column("Order_id")]
    [StringLength(20)]
    [Unicode(false)]
    public string OrderId { get; set; } = null!;

    [Column("Cart_id")]
    public int CartId { get; set; }

    [Column("Customer_id")]
    public int CustomerId { get; set; }

    [Column("Order_total_price")]
    public decimal? OrderTotalPrice { get; set; }

    [Column("Created_date", TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }
    
    [Column("Order_status")]
    public string OrderStatus { get; set; } = null!;

    [ForeignKey("CartId")]
    [InverseProperty("Orders")]
    public virtual Cart Cart { get; set; } = null!;

    [ForeignKey("CustomerId")]
    [InverseProperty("Orders")]
    public virtual Customer Customer { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Shipping> Shippings { get; set; } = new List<Shipping>();
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

public partial class Order
{
    [Key]
    [Column("Order_id")]
    [StringLength(20)]
    [Unicode(false)]
    public string OrderId { get; set; } = null!;

    [Column("Customer_id")]
    public int CustomerId { get; set; }

    [Column("Order_date", TypeName = "datetime")]
    public DateTime? OrderDate { get; set; }

    [Column("Total_amount")]
    public int? TotalAmount { get; set; }

    [Column("Order_status")]
    [StringLength(20)]
    public string OrderStatus { get; set; } = null!;

    [ForeignKey("CustomerId")]
    [InverseProperty("Orders")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("Order")]
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    [InverseProperty("Order")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [InverseProperty("Order")]
    public virtual ICollection<Shipping> Shippings { get; set; } = new List<Shipping>();
}

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
    public int OrderId { get; set; }

    [Column("Customer_id")]
    public int CustomerId { get; set; }

    [Column("Total_amount")]
    public int? TotalAmount { get; set; }

    [Column("Total_price", TypeName = "decimal(18, 2)")]
    public decimal? TotalPrice { get; set; }

    [StringLength(255)]
    public string Address { get; set; }
    public string Province { get; set; }
    public string District { get; set; }
    public string Ward { get; set; }
    public string Hamlet { get; set; }

    [Column("Transport_unit")]
    [StringLength(255)]
    public string? TransportUnit { get; set; }

    [Column("Shipping_code")]
    [StringLength(255)]
    public string? ShippingCode { get; set; }

    [Column("Created_date", TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column("Updated_Date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [Column("Order_status")]
    [StringLength(20)]
    public string OrderStatus { get; set; } = null!;

    [InverseProperty("Order")]
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    [ForeignKey("CustomerId")]
    [InverseProperty("Orders")]
    public virtual Customer Customer { get; set; } = null!;

    [InverseProperty("Order")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
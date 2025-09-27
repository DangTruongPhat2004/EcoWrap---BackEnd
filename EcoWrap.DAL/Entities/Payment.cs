using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Payment")]
[Index("TransactionId", Name = "UQ__Payment__9A8C4A3CC7284D51", IsUnique = true)]
public partial class Payment
{
    [Key]
    [Column("Payment_id")]
    public int PaymentId { get; set; }

    [Column("Order_id")]
    [StringLength(20)]
    [Unicode(false)]
    public string OrderId { get; set; } = null!;

    [Column("Payment_method_id")]
    public int PaymentMethodId { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }

    [Column("Payment_status")]
    [StringLength(20)]
    public string PaymentStatus { get; set; } = null!;

    [Column("Transaction_id")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TransactionId { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("Payments")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("PaymentMethodId")]
    [InverseProperty("Payments")]
    public virtual PaymentMethod PaymentMethod { get; set; } = null!;
}

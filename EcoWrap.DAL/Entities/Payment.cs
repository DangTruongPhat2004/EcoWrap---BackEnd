using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Payment")]
[Index("TransactionCode", Name = "UQ__Payment__5DB04AC31166E0C8", IsUnique = true)]
public partial class Payment
{
    [Key]
    [Column("Payment_id")]
    public int PaymentId { get; set; }

    [Column("Order_id")]
    public int? OrderId { get; set; }

    [Column(TypeName = "decimal(10, 2)")]
    public decimal Amount { get; set; }

    [Column("Payment_status")]
    [StringLength(20)]
    public string PaymentStatus { get; set; } = null!;

    [Column("Transaction_code")]
    [StringLength(100)]
    [Unicode(false)]
    public string? TransactionCode { get; set; }

    [ForeignKey("OrderId")]
    [InverseProperty("Payments")]
    public virtual Order? Order { get; set; }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("PaymentMethod")]
public partial class PaymentMethod
{
    [Key]
    [Column("Payment_method_id")]
    public int PaymentMethodId { get; set; }

    [Column("User_id")]
    public int UserId { get; set; }

    [Column("Method_name")]
    [StringLength(50)]
    public string MethodName { get; set; } = null!;

    [Column("Method_account")]
    [StringLength(255)]
    public string? MethodAccount { get; set; }

    [InverseProperty("PaymentMethod")]
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    [ForeignKey("UserId")]
    [InverseProperty("PaymentMethods")]
    public virtual User User { get; set; } = null!;
}

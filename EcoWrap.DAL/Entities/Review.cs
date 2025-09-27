using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Review")]
public partial class Review
{
    [Key]
    [Column("Review_id")]
    public int ReviewId { get; set; }

    [Column("Customer_id")]
    public int CustomerId { get; set; }

    [Column("Product_id")]
    [StringLength(15)]
    [Unicode(false)]
    public string ProductId { get; set; } = null!;

    public string? Comment { get; set; }

    public byte Rating { get; set; }

    [Column("Review_date", TypeName = "datetime")]
    public DateTime? ReviewDate { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Reviews")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("Reviews")]
    public virtual Product Product { get; set; } = null!;
}

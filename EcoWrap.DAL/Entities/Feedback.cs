using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Feedback")]
public partial class Feedback
{
    [Key]
    [Column("Feedback_id")]
    public int FeedbackId { get; set; }

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
    [InverseProperty("Feedbacks")]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey("ProductId")]
    [InverseProperty("Feedbacks")]
    public virtual Product Product { get; set; } = null!;
}

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

    [Column("User_id")]
    public int UserId { get; set; }
    public string? Comment { get; set; }
    public byte Rating { get; set; }

    [Column("Feedback_date", TypeName = "datetime")]
    public DateTime? FeedbackDate { get; set; }

    [ForeignKey("UserId")]
    [InverseProperty("Feedbacks")]
    public virtual User User { get; set; } = null!;
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

public partial class Collection
{
    [Key]
    [Column("Collection_id")]
    public int CollectionId { get; set; }

    [Column("Customer_id")]
    public int CustomerId { get; set; }

    [Column("Design_file")]
    public string? DesignFile { get; set; }

    [Column("Created_date", TypeName = "datetime")]
    public DateTime? CreatedDate { get; set; }

    [Column("Updated_date", TypeName = "datetime")]
    public DateTime? UpdatedDate { get; set; }

    [ForeignKey("CustomerId")]
    [InverseProperty("Collections")]
    public virtual Customer Customer { get; set; } = null!;
}

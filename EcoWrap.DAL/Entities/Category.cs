using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Table("Category")]
public partial class Category
{
    [Key]
    [Column("Category_id")]
    public int CategoryId { get; set; }

    [Column("Category_name")]
    [StringLength(50)]
    public string? CategoryName { get; set; }

    [InverseProperty("Category")]
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}

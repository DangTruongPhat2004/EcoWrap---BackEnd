using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

public partial class Template
{
    [Key]
    public int Id { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string? Category { get; set; }

    [StringLength(500)]
    public string? ThumbnailUrl { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DimensionsWidth { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DimensionsHeight { get; set; }

    [StringLength(10)]
    public string DimensionsUnit { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DefaultBleed { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DefaultSafeArea { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    [InverseProperty("Template")]
    public virtual ICollection<TemplateVersion> TemplateVersions { get; set; } = new List<TemplateVersion>();
}

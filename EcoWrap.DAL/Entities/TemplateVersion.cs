using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

public partial class TemplateVersion
{
    [Key]
    public int Id { get; set; }

    public int TemplateId { get; set; }

    public int VersionNo { get; set; }

    [StringLength(1000)]
    public string DielineSvgUrl { get; set; } = null!;

    [StringLength(128)]
    public string DielineHash { get; set; } = null!;

    [StringLength(500)]
    public string? Model3dUrl { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DimensionsWidth { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DimensionsHeight { get; set; }

    [StringLength(10)]
    public string DimensionsUnit { get; set; } = null!;

    [Column(TypeName = "decimal(18, 2)")]
    public decimal Bleed { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SafeArea { get; set; }

    [StringLength(400)]
    public string? Layers { get; set; }

    public DateTime CreatedAt { get; set; }

    [ForeignKey("TemplateId")]
    [InverseProperty("TemplateVersions")]
    public virtual Template Template { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Index("Category", "IsActive", Name = "IX_DesignAssets_Category_Active")]
[Index("CreatedAt", Name = "IX_DesignAssets_CreatedAt")]
[Index("Name", Name = "IX_DesignAssets_Name")]
[Index("Slug", Name = "UX_DesignAssets_Slug", IsUnique = true)]
public partial class DesignAsset
{
    [Key]
    public int Id { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(220)]
    public string Slug { get; set; } = null!;

    public byte Category { get; set; }

    [StringLength(600)]
    public string? Tags { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

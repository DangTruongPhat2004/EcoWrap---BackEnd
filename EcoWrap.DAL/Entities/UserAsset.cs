using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Index("UserId", "IsActive", "CreatedAt", Name = "IX_UserAssets_User_Active_CreatedAt", IsDescending = new[] { false, false, true })]
[Index("UserId", "Slug", Name = "UX_UserAssets_User_Slug", IsUnique = true)]
public partial class UserAsset
{
    [Key]
    public long Id { get; set; }

    public long UserId { get; set; }

    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(220)]
    public string Slug { get; set; } = null!;

    [StringLength(400)]
    public string PublicId { get; set; } = null!;

    [StringLength(20)]
    public string ResourceType { get; set; } = null!;

    [StringLength(20)]
    public string? Format { get; set; }

    [StringLength(800)]
    public string Url { get; set; } = null!;

    [StringLength(800)]
    public string? ThumbnailUrl { get; set; }

    [StringLength(100)]
    public string? ContentType { get; set; }

    public long Bytes { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    [StringLength(600)]
    public string? Tags { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}

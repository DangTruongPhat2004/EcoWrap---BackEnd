using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.DAL.Entities;

[Index("Sha256", Name = "IX_KbChunks_Sha256")]
public partial class KbChunk
{
    [Key]
    [StringLength(64)]
    public string Id { get; set; } = null!;

    [StringLength(300)]
    public string? Title { get; set; }

    [StringLength(500)]
    public string? Source { get; set; }

    [StringLength(200)]
    public string? Tags { get; set; }

    public string Text { get; set; } = null!;

    public byte[]? Embedding { get; set; }

    [StringLength(64)]
    public string? Sha256 { get; set; }

    public DateTime CreatedAt { get; set; }
}

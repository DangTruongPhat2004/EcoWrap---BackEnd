using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoWrap.DAL.Entities;

[Table("Designs")] // Đặt tên bảng DB là "Designs" cho module này
public class Custom
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long OwnerId { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = default!;

    [Required, MaxLength(24)]
    public string Status { get; set; } = "draft"; // draft | active | archived

    [MaxLength(2048)]
    public string? ThumbnailUrl { get; set; }

    [MaxLength(2048)]
    public string? DielineSvgUrl { get; set; }

    // Lưu Fabric.js scene JSON (kèm meta)
    public string DesignData { get; set; } = "{}";

    // JSON array các tag (string[])
    public string? TagsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(7);
    public DateTime? DeletedAt { get; set; }

    // Concurrency
    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<CustomAsset> Assets { get; set; } = new List<CustomAsset>();
}

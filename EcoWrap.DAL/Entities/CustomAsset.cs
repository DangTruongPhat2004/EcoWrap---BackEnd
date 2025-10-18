using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcoWrap.DAL.Entities;

[Table("CustomAssets")] // ✅ Bảng riêng, KHÔNG dính DesignAssets cũ
public class CustomAsset
{
    [Key]
    public long Id { get; set; }

    [Required]
    public long CustomId { get; set; }   // ✅ FK đặt tên rõ ràng theo entity Custom
    public Custom Custom { get; set; } = default!;

    [Required, MaxLength(2048)]
    public string Url { get; set; } = default!;

    [Required, MaxLength(16)]
    public string Source { get; set; } = default!; // user|library|template

    [Required, MaxLength(16)]
    public string Kind { get; set; } = default!;   // image|svg|pattern|font

    [MaxLength(128)] public string? AssetId { get; set; }
    [MaxLength(128)] public string? Mime { get; set; }

    public int? Width { get; set; }
    public int? Height { get; set; }

    [MaxLength(64)] public string? Version { get; set; }
}

namespace EcoWrap.Models.Designs;

public record UsedAssetDto
{
    public required string Url { get; init; }      // Cloudinary URL
    public required string Source { get; init; }   // user|library|template
    public required string Kind { get; init; }     // image|svg|pattern|font
    public string? AssetId { get; init; }
    public string? Mime { get; init; }
    public int? Width { get; init; }
    public int? Height { get; init; }
    public string? Version { get; init; }          // vNNNN… nếu cần
}

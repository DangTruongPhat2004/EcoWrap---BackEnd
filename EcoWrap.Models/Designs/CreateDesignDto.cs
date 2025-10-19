namespace EcoWrap.Models.Designs;

public record CreateDesignDto
{
    public required string Title { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? DielineSvgUrl { get; init; }

    // Fabric.js scene JSON (string hoá)
    public required string DesignData { get; init; }

    public List<UsedAssetDto>? UsedAssets { get; init; }
    public List<string>? Tags { get; init; }
}

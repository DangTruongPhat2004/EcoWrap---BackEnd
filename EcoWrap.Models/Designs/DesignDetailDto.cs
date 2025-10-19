namespace EcoWrap.Models.Designs;

public record DesignDetailDto
{
    public long Id { get; init; }
    public long OwnerId { get; init; }
    public string Title { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? ThumbnailUrl { get; init; }
    public string? DielineSvgUrl { get; init; }
    public string DesignData { get; init; } = default!;
    public List<string> Tags { get; init; } = new();
    public long Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<UsedAssetDto> UsedAssets { get; init; } = new();
}

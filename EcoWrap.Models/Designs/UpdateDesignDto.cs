namespace EcoWrap.Models.Designs;

public record UpdateDesignDto
{
    public string? Title { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? DielineSvgUrl { get; init; }
    public string? Status { get; init; }  // draft|active|archived

    // full replace cho autosave: gửi cả scene mới
    public string? DesignData { get; init; }

    public List<UsedAssetDto>? UsedAssets { get; init; }
    public List<string>? Tags { get; init; }
}

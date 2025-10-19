namespace EcoWrap.Models.Designs;

public record DesignListItemDto
{
    public long Id { get; init; }
    public string Title { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? ThumbnailUrl { get; init; }
    public List<string> Tags { get; init; } = new();
    public long Version { get; init; }           // từ RowVersion
    public DateTime UpdatedAt { get; init; }
}

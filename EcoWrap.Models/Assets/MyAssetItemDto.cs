// Models/Assets/MyAssetItemDto.cs
public class MyAssetItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string ThumbnailUrl { get; set; } = "";
    public string Format { get; set; } = "";
    public string ResourceType { get; set; } = "";   // image|raw
    public long Bytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
}

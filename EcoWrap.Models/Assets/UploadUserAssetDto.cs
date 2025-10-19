public class UploadUserAssetResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string ThumbnailUrl { get; set; } = "";
    public string Format { get; set; } = "";
    public long Bytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
}

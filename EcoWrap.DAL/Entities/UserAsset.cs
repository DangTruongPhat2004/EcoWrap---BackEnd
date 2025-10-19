public class UserAsset
{
    [Key]
    public long Id { get; set; }

    public long UserId { get; set; }

    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string PublicId { get; set; } = default!;     // u/{userId}/{slug}
    public string ResourceType { get; set; } = "image";  // image | raw
    public string Format { get; set; } = "";             // png/jpg/svg/pdf
    public string Url { get; set; } = default!;
    public string ThumbnailUrl { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long Bytes { get; set; }

    public int? Width { get; set; }

    public int? Height { get; set; }

    public string Tags { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

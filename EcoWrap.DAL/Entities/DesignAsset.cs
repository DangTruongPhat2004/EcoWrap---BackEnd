
public enum AssetCategory : byte
{
    Icon = 1,
    Pattern = 2,
    Illustration = 3
}

public class DesignAsset
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public AssetCategory Category { get; set; }
    public string Tags { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

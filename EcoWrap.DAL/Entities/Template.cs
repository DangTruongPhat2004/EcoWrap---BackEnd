using EcoWrap.DAL.Entities;


public class Template
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Category { get; set; }
    public string? ThumbnailUrl { get; set; }
    public decimal DimensionsWidth { get; set; }
    public decimal DimensionsHeight { get; set; }
    public string DimensionsUnit { get; set; } = "mm";
    public decimal DefaultBleed { get; set; } = 3;
    public decimal DefaultSafeArea { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

    // Quan trọng để fix lỗi .Versions
    public  ICollection<TemplateVersion> Versions { get; set; } = new List<TemplateVersion>();
}
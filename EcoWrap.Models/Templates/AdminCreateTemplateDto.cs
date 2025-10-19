namespace EcoWrap.Models.Templates;

public sealed class AdminCreateTemplateDto
{
    public string Name { get; set; } = default!;
    public string? Category { get; set; }
    public string? ThumbnailUrl { get; set; }

    // mm
    public decimal DimensionsWidth { get; set; }
    public decimal DimensionsHeight { get; set; }
    public string DimensionsUnit { get; set; } = "mm";

    public decimal DefaultBleed { get; set; } = 3m;
    public decimal DefaultSafeArea { get; set; } = 5m;

    public bool IsActive { get; set; } = true;
}

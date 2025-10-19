namespace EcoWrap.Models.Templates;

public sealed class AdminCreateTemplateVersionDto
{
    // Nếu muốn override thông số so với Template
    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public string? Unit { get; set; }  // "mm"
    public decimal? Bleed { get; set; }
    public decimal? SafeArea { get; set; }
    public string? LayersCsv { get; set; } // "cut,crease,fold,print-guides"
    public string? Model3dUrl { get; set; }
}

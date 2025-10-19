public class TemplateVersion
{
    public int Id { get; set; }               // Quan trọng để fix lỗi .Id
    public int TemplateId { get; set; }

    public int VersionNo { get; set; }

    public string DielineSvgUrl { get; set; } = default!;
    public string DielineHash { get; set; } = default!;
    public string? Model3dUrl { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DimensionsWidth { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal DimensionsHeight { get; set; }
    public string DimensionsUnit { get; set; } = "mm";
    public decimal Bleed { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal SafeArea { get; set; }

    [StringLength(400)]
    public string? Layers { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Template Template { get; set; } = default!;
}

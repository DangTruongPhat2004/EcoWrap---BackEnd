namespace EcoWrap.Models.Templates;

public sealed class AdminPullTemplateVersionDto
{
    public string SourceUrl { get; set; } = default!;
    public AdminCreateTemplateVersionDto? Override { get; set; }
}

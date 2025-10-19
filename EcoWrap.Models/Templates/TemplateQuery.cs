namespace EcoWrap.Models.Templates;

public sealed class TemplateQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Category { get; init; }
    public string? Sort { get; init; } // name_asc|name_desc|createdAt_desc

    public (int page, int size) Normalize()
    {
        var p = Page < 1 ? 1 : Page;
        var s = PageSize < 1 ? 1 : (PageSize > 100 ? 100 : PageSize);
        return (p, s);
    }
}

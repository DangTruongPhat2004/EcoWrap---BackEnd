namespace EcoWrap.Models.Templates;

public record TemplateListItemDto(
    int Id,
    string Name,
    string? ThumbnailUrl,
    DimDto Dimensions,
    string? Category,
    bool IsActive,
    int Version // latest version
);

public record TemplateDetailDto(
    int Id,
    string Name,
    DimDto Dimensions,
    decimal Bleed,
    decimal SafeArea,
    string DielineSvgUrl,
    string? Model3dUrl,
    int Version,
    string[] Layers,
    string? Category,
    bool IsActive
);

public record DimDto(decimal Width, decimal Height, string Unit);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long Total);

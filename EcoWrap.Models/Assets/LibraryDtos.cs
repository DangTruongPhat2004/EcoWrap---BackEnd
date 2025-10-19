public class LibraryQueryDto
{
    public string? Category { get; set; }  // icons|patterns|illustrations
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
public class LibraryItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string Url { get; set; } = default!;
    public string ThumbnailUrl { get; set; } = default!;
    public string[] Tags { get; set; } = Array.Empty<string>();
}
public class PagedResponse<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long Total { get; init; }
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
    public bool HasNext => Page < TotalPages;
}
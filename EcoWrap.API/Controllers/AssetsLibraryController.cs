using EcoWrap.DAL;
using EcoWrap.DAL.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("assets/library")]
public class AssetsLibraryController : ControllerBase
{
    private readonly EcoWrapDbContext _db;
    private readonly IValidator<LibraryQueryDto> _validator;
    private readonly IAssetUrlBuilder _url;

    private static readonly Dictionary<string, AssetCategory> CatMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["icon"] = AssetCategory.Icon,
        ["icons"] = AssetCategory.Icon,
        ["pattern"] = AssetCategory.Pattern,
        ["patterns"] = AssetCategory.Pattern,
        ["illustration"] = AssetCategory.Illustration,
        ["illustrations"] = AssetCategory.Illustration
    };

    public AssetsLibraryController(EcoWrapDbContext db, IValidator<LibraryQueryDto> validator, IAssetUrlBuilder url)
    {
        _db = db; _validator = validator; _url = url;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<LibraryItemDto>>> Get([FromQuery] LibraryQueryDto q, CancellationToken ct)
    {
        var val = await _validator.ValidateAsync(q, ct);
        if (!val.IsValid)
        {
            foreach (var e in val.Errors)
                ModelState.AddModelError(e.PropertyName, e.ErrorMessage);
            return ValidationProblem(ModelState); // ✅
        }

        var query = _db.Set<DesignAsset>()
                 .AsNoTracking()
                 .Where(a => a.IsActive);


        if (!string.IsNullOrWhiteSpace(q.Category) && CatMap.TryGetValue(q.Category!, out var cat))
            query = query.Where(a => a.Category == cat);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(a => EF.Functions.Like(a.Name, $"%{s}%") || EF.Functions.Like(a.Tags, $"%{s}%"));
        }

        var total = await query.LongCountAsync(ct);

        var rows = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(a => new { a.Id, a.Name, a.Category, a.Slug, a.Tags })
            .ToListAsync(ct);

        var items = rows.Select(a =>
        {
            var catStr = a.Category.ToString().ToLower();       // ✅ string
            var (url, thumb) = _url.Build(catStr, a.Slug);
            return new LibraryItemDto
            {
                Id = a.Id,
                Name = a.Name,
                Category = catStr,                               // ✅ map đúng kiểu
                Url = url,
                ThumbnailUrl = thumb,
                Tags = (a.Tags ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            };
        }).ToList();

        return Ok(new PagedResponse<LibraryItemDto>
        {
            Items = items,
            Page = q.Page,
            PageSize = q.PageSize,
            Total = total
        });
    }
}

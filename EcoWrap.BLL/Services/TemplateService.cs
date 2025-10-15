using EcoWrap.DAL;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Templates;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.BLL.Services;

public class TemplateService : ITemplateService
{
    private readonly EcoWrapDbContext _db;

    public TemplateService(EcoWrapDbContext db) => _db = db;

    public async Task<PagedResult<TemplateListItemDto>> ListAsync(TemplateQuery q, CancellationToken ct = default)
    {
        var (page, size) = q.Normalize();

        var baseQ = _db.Templates.AsNoTracking().Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(q.Category))
            baseQ = baseQ.Where(t => t.Category != null && t.Category == q.Category);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            baseQ = baseQ.Where(t => EF.Functions.Like(t.Name, $"%{s}%"));
        }

        // sort
        baseQ = q.Sort switch
        {
            "name_asc" => baseQ.OrderBy(t => t.Name),
            "name_desc" => baseQ.OrderByDescending(t => t.Name),
            _ => baseQ.OrderByDescending(t => t.CreatedAt) // createdAt_desc (default)
        };

        var total = await baseQ.LongCountAsync(ct);
        var pageItems = await baseQ
            .Skip((page - 1) * size)
            .Take(size)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.ThumbnailUrl,
                t.DimensionsWidth,
                t.DimensionsHeight,
                t.DimensionsUnit,
                t.Category,
                t.IsActive
            })
            .ToListAsync(ct);

        // lấy version mới nhất cho các template trong trang
        var ids = pageItems.Select(x => x.Id).ToList();
        var latestVersions = await _db.TemplateVersions.AsNoTracking()
            .Where(v => ids.Contains(v.TemplateId))
            .GroupBy(v => v.TemplateId)
            .Select(g => new { TemplateId = g.Key, Version = g.Max(x => x.VersionNo) })
            .ToDictionaryAsync(x => x.TemplateId, x => x.Version, ct);

        var items = pageItems.Select(x =>
            new TemplateListItemDto(
                x.Id,
                x.Name,
                x.ThumbnailUrl,
                new DimDto(x.DimensionsWidth, x.DimensionsHeight, x.DimensionsUnit),
                x.Category,
                x.IsActive,
                latestVersions.TryGetValue(x.Id, out var ver) ? ver : 0
            )
        ).ToList();

        return new PagedResult<TemplateListItemDto>(items, page, size, total);
    }

    public async Task<(TemplateDetailDto dto, string etag)?> GetDetailAsync(int id, CancellationToken ct = default)
    {
        // Template + version mới nhất
        var tpl = await _db.Templates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id && t.IsActive, ct);
        if (tpl == null) return null;

        var ver = await _db.TemplateVersions.AsNoTracking()
            .Where(v => v.TemplateId == id)
            .OrderByDescending(v => v.VersionNo)
            .FirstOrDefaultAsync(ct);

        if (ver == null) return null; // không có version => không hợp lệ để FE dùng

        var dto = new TemplateDetailDto(
            tpl.Id,
            tpl.Name,
            new DimDto(ver.DimensionsWidth, ver.DimensionsHeight, ver.DimensionsUnit),
            ver.Bleed,
            ver.SafeArea,
            ver.DielineSvgUrl,
            ver.Model3dUrl,
            ver.VersionNo,
            ver.Layers.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            tpl.Category,
            tpl.IsActive
        );

        // ETag: dùng DielineHash + VersionNo (ổn định, nhẹ)
        var etag = $"W/\"{ver.DielineHash}-v{ver.VersionNo}\"";
        return (dto, etag);
    }
}

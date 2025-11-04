using System.Text.Json;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Designs;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.BLL.Services;

public class CustomDesignsService : ICustomDesignsService
{
    private readonly EcoWrapDbContext _db;
    public CustomDesignsService(EcoWrapDbContext db) => _db = db;

    // ===== LIST =====
    public async Task<(IReadOnlyList<DesignListItemDto> items, int total)> GetListAsync(
    long ownerId, int page, int pageSize, string? q, string? status, CancellationToken ct)
    {
        var query = _db.Customs.AsNoTracking()
            .Where(d => d.OwnerId == ownerId && d.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(d => d.Title.Contains(q));
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(d => d.Status == status);

        var total = await query.CountAsync(ct);

        // 1) Chỉ select dữ liệu thô có thể dịch sang SQL
        var rows = await query
            .OrderByDescending(d => d.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new
            {
                d.Id,
                d.Title,
                d.Status,
                d.ThumbnailUrl,
                d.TagsJson,
                d.RowVersion,
                d.UpdatedAt
            })
            .ToListAsync(ct);

        // 2) Map sang DTO trong bộ nhớ (OK gọi JsonSerializer/BitConverter)
        var items = rows.Select(d => new DesignListItemDto
        {
            Id = d.Id,
            Title = d.Title,
            Status = d.Status,
            ThumbnailUrl = d.ThumbnailUrl,
            Tags = d.TagsJson != null
                ? System.Text.Json.JsonSerializer.Deserialize<List<string>>(d.TagsJson)!
                : new List<string>(),
            Version = d.RowVersion != null
                ? BitConverter.ToInt64(d.RowVersion.Take(8).ToArray(), 0)
                : 0,
            UpdatedAt = d.UpdatedAt
        }).ToList();

        return (items, total);
    }


    // ===== GET BY ID =====
    public async Task<(DesignDetailDto dto, byte[] rowVersion)?> GetAsync(long id, long ownerId, CancellationToken ct)
    {
        var d = await _db.Customs
            .Include(x => x.Assets)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId && x.DeletedAt == null, ct);

        if (d == null) return null;

        var dto = ToDetailDto(d);
        return (dto, d.RowVersion);
    }

    // ===== CREATE =====
    public async Task<(DesignDetailDto dto, byte[] rowVersion)> CreateAsync(
        long ownerId, CreateDesignDto body, CancellationToken ct)
    {
        var d = new Custom
        {
            OwnerId = ownerId,
            Title = body.Title,
            Status = "draft",
            ThumbnailUrl = body.ThumbnailUrl,
            DielineSvgUrl = body.DielineSvgUrl,
            DesignData = body.DesignData,
            TagsJson = body.Tags is { Count: > 0 } ? JsonSerializer.Serialize(body.Tags) : null,
            CreatedAt = DateTime.UtcNow.AddHours(7),
            UpdatedAt = DateTime.UtcNow.AddHours(7)
        };

        _db.Customs.Add(d);
        await _db.SaveChangesAsync(ct); // Id + RowVersion

        if (body.UsedAssets is { Count: > 0 })
        {
            foreach (var a in body.UsedAssets)
            {
                _db.CustomAssets.Add(new CustomAsset
                {
                    CustomId = d.Id,
                    Url = a.Url,
                    Source = a.Source,
                    Kind = a.Kind,
                    AssetId = a.AssetId,
                    Mime = a.Mime,
                    Width = a.Width,
                    Height = a.Height,
                    Version = a.Version
                });
            }
            await _db.SaveChangesAsync(ct);
        }

        // reload with assets
        var fresh = await _db.Customs.Include(x => x.Assets)
            .FirstAsync(x => x.Id == d.Id, ct);

        return (ToDetailDto(fresh), fresh.RowVersion);
    }

    // ===== UPDATE (Optimistic Concurrency) =====
    public async Task<(DesignDetailDto dto, byte[] rowVersion)?> UpdateAsync(
        long id, long ownerId, byte[] ifMatch, UpdateDesignDto body, CancellationToken ct)
    {
        var d = await _db.Customs.Include(x => x.Assets)
            .FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId && x.DeletedAt == null, ct);
        if (d == null) return null;

        // Kiểm tra RowVersion khớp (ETag/If-Match)
        if (!d.RowVersion.SequenceEqual(ifMatch))
            throw new DbUpdateConcurrencyException("ETag mismatch");

        if (body.Title != null) d.Title = body.Title;
        if (body.Status != null) d.Status = body.Status;
        if (body.ThumbnailUrl != null) d.ThumbnailUrl = body.ThumbnailUrl;
        if (body.DielineSvgUrl != null) d.DielineSvgUrl = body.DielineSvgUrl;
        if (body.DesignData != null) d.DesignData = body.DesignData;
        if (body.Tags != null) d.TagsJson = body.Tags.Count > 0 ? JsonSerializer.Serialize(body.Tags) : null;

        if (body.UsedAssets != null)
        {
            // Full replace semantics — đơn giản, an toàn cho autosave
            _db.CustomAssets.RemoveRange(d.Assets);
            if (body.UsedAssets.Count > 0)
            {
                foreach (var a in body.UsedAssets)
                {
                    _db.CustomAssets.Add(new CustomAsset
                    {
                        CustomId = d.Id,
                        Url = a.Url,
                        Source = a.Source,
                        Kind = a.Kind,
                        AssetId = a.AssetId,
                        Mime = a.Mime,
                        Width = a.Width,
                        Height = a.Height,
                        Version = a.Version
                    });
                }
            }
        }

        d.UpdatedAt = DateTime.UtcNow.AddHours(7);
        await _db.SaveChangesAsync(ct);

        var fresh = await _db.Customs.Include(x => x.Assets)
            .FirstAsync(x => x.Id == d.Id, ct);

        return (ToDetailDto(fresh), fresh.RowVersion);
    }

    // ===== DELETE =====
    public async Task<bool> DeleteAsync(long id, long ownerId, bool hard, CancellationToken ct)
    {
        var d = await _db.Customs.FirstOrDefaultAsync(x => x.Id == id && x.OwnerId == ownerId && x.DeletedAt == null, ct);
        if (d == null) return false;

        if (hard)
        {
            _db.Customs.Remove(d);
        }
        else
        {
            d.DeletedAt = DateTime.UtcNow.AddHours(7);
        }

        await _db.SaveChangesAsync(ct);
        return true;
    }

    // ===== Mapping helper =====
    private static DesignDetailDto ToDetailDto(Custom d)
    {
        return new DesignDetailDto
        {
            Id = d.Id,
            OwnerId = d.OwnerId,
            Title = d.Title,
            Status = d.Status,
            ThumbnailUrl = d.ThumbnailUrl,
            DielineSvgUrl = d.DielineSvgUrl,
            DesignData = d.DesignData,
            Tags = d.TagsJson != null ? JsonSerializer.Deserialize<List<string>>(d.TagsJson)! : new(),
            Version = d.RowVersion != null ? BitConverter.ToInt64(d.RowVersion.Take(8).ToArray(), 0) : 0,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt,
            UsedAssets = d.Assets.Select(a => new UsedAssetDto
            {
                Url = a.Url,
                Source = a.Source,
                Kind = a.Kind,
                AssetId = a.AssetId,
                Mime = a.Mime,
                Width = a.Width,
                Height = a.Height,
                Version = a.Version
            }).ToList()
        };
    }
}

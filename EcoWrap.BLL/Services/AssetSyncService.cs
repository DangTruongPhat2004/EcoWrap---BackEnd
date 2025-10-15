using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EcoWrap.DAL;
using EcoWrap.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

public class AssetSyncService
{
    private readonly Cloudinary _cloud;
    private readonly EcoWrapDbContext _db;
    private readonly IConfiguration _cfg;

    public AssetSyncService(EcoWrapDbContext db, IConfiguration cfg)
    {
        _db = db;
        _cfg = cfg;

        var cloud = _cfg["Storage:Cloudinary:CloudName"]!;
        var key = _cfg["Storage:Cloudinary:ApiKey"]!;
        var sec = _cfg["Storage:Cloudinary:ApiSecret"]!;
        _cloud = new Cloudinary(new Account(cloud, key, sec));
    }

    public async Task<AssetSyncResult> SyncAsync(CancellationToken ct)
    {
        var root = (_cfg["Storage:Cloudinary:AssetsFolder"] ?? "Library").Trim('/');
        var subIcons = _cfg["Storage:Cloudinary:AssetsSubfolders:icon"] ?? "icons";
        var subPatterns = _cfg["Storage:Cloudinary:AssetsSubfolders:pattern"] ?? "patterns";
        var subIllustrations = _cfg["Storage:Cloudinary:AssetsSubfolders:illustration"] ?? "illustrations";

        var prefixes = new List<(AssetCategory cat, string prefix)>
        {
            (AssetCategory.Icon,         $"{root}/{subIcons}/"),
            (AssetCategory.Pattern,      $"{root}/{subPatterns}/"),
            (AssetCategory.Illustration, $"{root}/{subIllustrations}/")
        };

        int created = 0, updated = 0, scanned = 0;

        foreach (var (cat, prefix) in prefixes)
        {
            // image trước
            var r1 = await SyncPrefix(ResourceType.Image, cat, prefix, ct);
            created += r1.created; updated += r1.updated; scanned += r1.scanned;

            // raw sau
            var r2 = await SyncPrefix(ResourceType.Raw, cat, prefix, ct);
            created += r2.created; updated += r2.updated; scanned += r2.scanned;
        }

        return new AssetSyncResult { Created = created, Updated = updated, Scanned = scanned };
    }

    private async Task<(int created, int updated, int scanned)> SyncPrefix(
        ResourceType rt, AssetCategory cat, string prefix, CancellationToken ct)
    {
        int created = 0, updated = 0, scanned = 0;
        string? cursor = null;

        do
        {
            var list = await _cloud.ListResourcesAsync(new ListResourcesByPrefixParams
            {
                ResourceType = rt,
                Type = "upload",
                Prefix = prefix,     // ví dụ: Library/icons/
                MaxResults = 500,
                NextCursor = cursor
            });

            cursor = list.NextCursor;

            foreach (var r in list.Resources)
            {
                ct.ThrowIfCancellationRequested();
                scanned++;

                // PublicId = "Library/icons/leaf-outline"
                var slug = r.PublicId.AsSpan(r.PublicId.LastIndexOf('/') + 1).ToString();
                var name = ToTitle(slug);
                var tags = (r.Tags is { Length: > 0 }) ? string.Join(',', r.Tags) : "";

                var existing = await _db.Set<DesignAsset>()
                    .FirstOrDefaultAsync(x => x.Slug == slug, ct);

                if (existing is null)
                {
                    _db.Add(new DesignAsset
                    {
                        Name = name,
                        Slug = slug,
                        Category = cat,
                        Tags = tags,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    created++;
                }
                else
                {
                    var changed = false;

                    if (string.IsNullOrWhiteSpace(existing.Name))
                    { existing.Name = name; changed = true; }

                    if (string.IsNullOrWhiteSpace(existing.Tags) && !string.IsNullOrWhiteSpace(tags))
                    { existing.Tags = tags; changed = true; }

                    if (existing.Category != cat)
                    { existing.Category = cat; changed = true; }

                    if (changed) updated++;
                }
            }

            if (created > 0 || updated > 0)
                await _db.SaveChangesAsync(ct);

        } while (!string.IsNullOrEmpty(cursor));

        return (created, updated, scanned);
    }

    private static string ToTitle(string slug)
    {
        var parts = slug.Split('-', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            var s = parts[i];
            parts[i] = char.ToUpperInvariant(s[0]) + (s.Length > 1 ? s[1..] : "");
        }
        return string.Join(' ', parts);
    }
}

public class AssetSyncResult
{
    public int Created { get; set; }
    public int Updated { get; set; }
    public int Scanned { get; set; }
}

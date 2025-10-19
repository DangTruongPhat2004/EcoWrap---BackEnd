// Controllers/UserAssetsController.cs
#nullable enable
using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using EcoWrap.DAL.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("assets")]
[Authorize]
public class UserAssetsController : ControllerBase
{
    private readonly UserAssetUploader _uploader;
    private readonly EcoWrapDbContext _db;
    private readonly IValidator<MyAssetsQueryDto> _validator;
    private readonly IConfiguration _cfg;
    private readonly Cloudinary _cloud;

    public UserAssetsController(
        UserAssetUploader uploader,
        EcoWrapDbContext db,
        IValidator<MyAssetsQueryDto> validator,
        IConfiguration cfg,
        Cloudinary cloud)
    {
        _uploader = uploader;
        _db = db;
        _cfg = cfg;
        _validator = validator;
        _cloud = cloud;
    }

    // ✅ Helper: lấy userId (long) từ NameIdentifier / sub / uid
    private bool TryGetUserId(out long userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub")
                  ?? User.FindFirstValue("uid");
        return long.TryParse(raw, out userId);
    }

    [HttpPost("upload")]
    [RequestSizeLimit(6_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult> Upload([FromForm] UploadUserAssetRequest form, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (form.Files is null || form.Files.Count == 0)
            return BadRequest(new { error = "at least one file is required" });

        var results = new List<UploadUserAssetResponse>();
        foreach (var file in form.Files)
        {
            try
            {
                var name = Path.GetFileNameWithoutExtension(file.FileName);
                // 🔁 Đổi sang long userId
                var res = await _uploader.UploadAsync(userId, file, name, form.Tags, ct);
                results.Add(res);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = $"Failed to upload {file.FileName}: {ex.Message}" });
            }
        }
        return Ok(results);
    }

    [HttpGet("my-assets")]
    public async Task<ActionResult<PagedResponse<MyAssetItemDto>>> GetMyAssets([FromQuery] MyAssetsQueryDto q, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var val = await _validator.ValidateAsync(q, ct);
        if (!val.IsValid) return ValidationProblem(new ValidationProblemDetails(val.ToDictionary()));

        // ❗Bỏ q.UserId — luôn dùng userId từ token
        var query = _db.UserAssets.AsNoTracking()
            .Where(a => a.UserId == userId && a.IsActive);

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(a => EF.Functions.Like(a.Name, $"%{s}%") || EF.Functions.Like(a.Tags!, $"%{s}%"));
        }
        if (!string.IsNullOrWhiteSpace(q.Format))
        {
            var f = q.Format.Trim().ToLower();
            query = query.Where(a => a.Format.ToLower() == f);
        }
        if (!string.IsNullOrWhiteSpace(q.Type))
        {
            var t = q.Type.Trim().ToLower(); // image|raw
            query = query.Where(a => a.ResourceType.ToLower() == t);
        }

        var total = await query.LongCountAsync(ct);

        var rows = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(a => new MyAssetItemDto
            {
                Id = a.Id,
                Name = a.Name,
                Url = a.Url,
                ThumbnailUrl = a.ThumbnailUrl ?? a.Url,
                Format = a.Format,
                ResourceType = a.ResourceType,
                Bytes = a.Bytes,
                Width = a.Width,
                Height = a.Height,
                Tags = (a.Tags ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new PagedResponse<MyAssetItemDto>
        {
            Items = rows,
            Page = q.Page,
            PageSize = q.PageSize,
            Total = total
        });
    }

    [HttpDelete("my-assets/{id:long}")]
    public async Task<ActionResult> DeleteMyAsset(long id, [FromQuery] bool purge = false, CancellationToken ct = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var asset = await _db.UserAssets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (asset is null) return NotFound(new { error = "asset not found" });

        var alreadyInactive = !asset.IsActive;
        asset.IsActive = false;
        await _db.SaveChangesAsync(ct);

        var purged = false;
        if (purge)
        {
            var destroyParams = new DeletionParams(asset.PublicId)
            {
                ResourceType = asset.ResourceType?.ToLower() == "raw" ? ResourceType.Raw : ResourceType.Image,
                Invalidate = true
            };
            var delRes = await _cloud.DestroyAsync(destroyParams);
            purged = delRes.StatusCode == System.Net.HttpStatusCode.OK &&
                     (delRes.Result == "ok" || delRes.Result == "not found");
        }

        return Ok(new { id = asset.Id, deleted = true, alreadyInactive, purged });
    }

    [HttpPost("my-assets/{id:long}/restore")]
    public async Task<ActionResult> RestoreMyAsset(long id, CancellationToken ct)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var asset = await _db.UserAssets.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, ct);
        if (asset is null) return NotFound(new { error = "asset not found" });
        if (asset.IsActive) return BadRequest(new { error = "asset is already active" });

        asset.IsActive = true;
        await _db.SaveChangesAsync(ct);
        return Ok(new { id = asset.Id, restored = true });
    }
}

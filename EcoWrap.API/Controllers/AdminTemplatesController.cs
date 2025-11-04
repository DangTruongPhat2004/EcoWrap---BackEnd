using EcoWrap.DAL;
using EcoWrap.DAL.Entities;
using EcoWrap.Infrastructure.Sanitization;
using EcoWrap.Infrastructure.Storage;
using EcoWrap.Models.Templates;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.API.Controllers;

[ApiController]
[Route("admin/templates")]
public class AdminTemplatesController : ControllerBase
{
    private readonly EcoWrapDbContext _db;
    private readonly IValidator<AdminCreateTemplateDto> _createValidator;
    private readonly IFileStorage _storage;
    private readonly IConfiguration _cfg;
    private readonly ILogger<AdminTemplatesController> _logger;
    private readonly ISvgSanitizer _sanitizer;

    public AdminTemplatesController(
        EcoWrapDbContext db,
        IValidator<AdminCreateTemplateDto> createValidator,
        IFileStorage storage,
        IConfiguration cfg,
        ILogger<AdminTemplatesController> logger,
        ISvgSanitizer sanitizer)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _cfg = cfg ?? throw new ArgumentNullException(nameof(cfg));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
    }

    // 1) POST /admin/templates  (tạo template metadata)
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] AdminCreateTemplateDto dto, CancellationToken ct)
    {
        var vr = await _createValidator.ValidateAsync(dto, ct);
        if (!vr.IsValid)
        {
            var modelState = new ModelStateDictionary();
            foreach (var e in vr.Errors)
                modelState.AddModelError(e.PropertyName, e.ErrorMessage);

            return ValidationProblem(modelState);
        }

        var t = new Template
        {
            Name = dto.Name.Trim(),
            Category = dto.Category?.Trim(),
            ThumbnailUrl = dto.ThumbnailUrl?.Trim(),
            DimensionsWidth = dto.DimensionsWidth,
            DimensionsHeight = dto.DimensionsHeight,
            DimensionsUnit = dto.DimensionsUnit,
            DefaultBleed = dto.DefaultBleed,
            DefaultSafeArea = dto.DefaultSafeArea,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow.AddHours(7)
        };
        _db.Templates.Add(t);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(Get), new { id = t.Id }, new { id = t.Id });
    }

    // tiện dụng: GET 1 template (để CreatedAtAction trỏ tới)
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken ct)
    {
        var t = await _db.Templates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return t is null ? NotFound() : Ok(new { t.Id, t.Name, t.IsActive, t.ThumbnailUrl});
    }

    // 2) POST /admin/templates/{id}/versions   (multipart: file=SVG, + optional fields)
    //[Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/versions")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVersion(
        [FromRoute] int id,
        [FromForm] CreateTemplateVersionFormDto form,
        CancellationToken ct)
    {
        var tpl = await _db.Templates.FindAsync(new object?[] { id }, ct);
        if (tpl is null || !tpl.IsActive) return NotFound();

        if (form.File is null || form.File.Length == 0)
            return Problem(statusCode: 400, title: "SVG file is required");

        var contentType = (form.File.ContentType ?? "").ToLowerInvariant();
        var fileName = form.File.FileName ?? "";
        if (!contentType.Contains("svg") && !fileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return Problem(statusCode: 415, title: "Only SVG files are accepted");

        await using var raw = form.File.OpenReadStream();

        Stream src;
        try
        {
            src = await _sanitizer.SanitizeAsync(raw, ct);
            if (src.CanSeek) src.Position = 0;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Invalid SVG uploaded for template {Id}", id);
            return Problem(statusCode: 400, title: "Invalid SVG", detail: ex.Message);
        }

        var latest = await _db.TemplateVersions
            .Where(v => v.TemplateId == id)
            .OrderByDescending(v => v.VersionNo)
            .FirstOrDefaultAsync(ct);
        var nextNo = (latest?.VersionNo ?? 0) + 1;

        var prefix = _cfg["Storage:DielinePrefix"] ?? "dielines";
        var key = $"{prefix}/{id}/v{nextNo}.svg";
        var stored = await _storage.SaveAsync(src, "image/svg+xml", key, immutableCache: true, ct);

        if (stored is null || string.IsNullOrWhiteSpace(stored.PublicUrl))
            return Problem(statusCode: 502, title: "Upload SVG failed (empty URL).");

        // Chuẩn hoá input (không đụng kiểu số để tránh lỗi build)
        var unit = NormalizeUnit(form.Unit) ?? tpl.DimensionsUnit;
        var layers = NormalizeLayersCsv(form.LayersCsv) ?? "cut,crease,fold,print-guides";

        var w = form.Width ?? tpl.DimensionsWidth;
        var h = form.Height ?? tpl.DimensionsHeight;
        var bleed = form.Bleed ?? tpl.DefaultBleed;
        var safe = form.SafeArea ?? tpl.DefaultSafeArea;

        var ver = new TemplateVersion
        {
            TemplateId = id,
            VersionNo = nextNo,
            DielineSvgUrl = ToHttps(stored.PublicUrl),
            DielineHash = string.IsNullOrWhiteSpace(stored.ContentHash) ? null : stored.ContentHash,
            Model3dUrl = AsNullIfPlaceholder(form.Model3dUrl),
            DimensionsWidth = w,
            DimensionsHeight = h,
            DimensionsUnit = unit,
            Bleed = bleed,
            SafeArea = safe,
            Layers = layers,
            CreatedAt = DateTime.UtcNow.AddHours(7)
        };

        _db.TemplateVersions.Add(ver);
        await _db.SaveChangesAsync(ct);

        return Created(string.Empty, new
        {
            templateId = ver.TemplateId,
            versionNo = ver.VersionNo,
            dielineSvgUrl = ver.DielineSvgUrl,
            dielineHash = ver.DielineHash,
            dimensions = new { width = ver.DimensionsWidth, height = ver.DimensionsHeight, unit = ver.DimensionsUnit },
            bleed = ver.Bleed,
            safeArea = ver.SafeArea,
            layers = (ver.Layers ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            createdAt = ver.CreatedAt
        });
    }

    // 3) (optional) POST /admin/templates/{id}/versions/pull   (tải từ URL)
    //[Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/versions/pull")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> PullVersion([FromRoute] int id, [FromBody] AdminPullTemplateVersionDto body, CancellationToken ct)
    {
        var tpl = await _db.Templates.FindAsync(new object?[] { id }, ct);
        if (tpl is null || !tpl.IsActive) return NotFound();
        if (string.IsNullOrWhiteSpace(body.SourceUrl))
            return Problem(statusCode: 400, title: "sourceUrl is required");

        using var http = new HttpClient();
        using var resp = await http.GetAsync(body.SourceUrl, ct);
        if (!resp.IsSuccessStatusCode) return Problem(statusCode: 400, title: "Cannot fetch sourceUrl");
        var ctType = resp.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() ?? "";
        if (!ctType.Contains("svg") && !body.SourceUrl.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            return Problem(statusCode: 415, title: "Only SVG files are accepted");

        await using var raw = await resp.Content.ReadAsStreamAsync(ct);

        Stream src;
        try
        {
            src = await _sanitizer.SanitizeAsync(raw, ct);
            if (src.CanSeek) src.Position = 0;
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Invalid SVG pulled for template {Id}", id);
            return Problem(statusCode: 400, title: "Invalid SVG", detail: ex.Message);
        }

        var latest = await _db.TemplateVersions
            .Where(v => v.TemplateId == id)
            .OrderByDescending(v => v.VersionNo)
            .FirstOrDefaultAsync(ct);
        var nextNo = (latest?.VersionNo ?? 0) + 1;

        var prefix = _cfg["Storage:DielinePrefix"] ?? "dielines";
        var key = $"{prefix}/{id}/v{nextNo}.svg";
        var stored = await _storage.SaveAsync(src, "image/svg+xml", key, immutableCache: true, ct);

        if (stored is null || string.IsNullOrWhiteSpace(stored.PublicUrl))
            return Problem(statusCode: 502, title: "Upload SVG failed (empty URL).");

        var ov = body.Override;
        var unit = NormalizeUnit(ov?.Unit) ?? tpl.DimensionsUnit;
        var layers = NormalizeLayersCsv(ov?.LayersCsv) ?? "cut,crease,fold,print-guides";

        var ver = new TemplateVersion
        {
            TemplateId = id,
            VersionNo = nextNo,
            DielineSvgUrl = ToHttps(stored.PublicUrl),
            DielineHash = string.IsNullOrWhiteSpace(stored.ContentHash) ? null : stored.ContentHash,
            Model3dUrl = AsNullIfPlaceholder(ov?.Model3dUrl),
            DimensionsWidth = ov?.Width ?? tpl.DimensionsWidth,
            DimensionsHeight = ov?.Height ?? tpl.DimensionsHeight,
            DimensionsUnit = unit,
            Bleed = ov?.Bleed ?? tpl.DefaultBleed,
            SafeArea = ov?.SafeArea ?? tpl.DefaultSafeArea,
            Layers = layers,
            CreatedAt = DateTime.UtcNow.AddHours(7)
        };

        _db.TemplateVersions.Add(ver);
        await _db.SaveChangesAsync(ct);

        return Created(string.Empty, new
        {
            templateId = ver.TemplateId,
            versionNo = ver.VersionNo,
            dielineSvgUrl = ver.DielineSvgUrl
        });
    }

    // ===== Helpers (string-only để không đụng kiểu số) =====
    private static string? AsNullIfPlaceholder(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim();
        return t.Equals("string", StringComparison.OrdinalIgnoreCase) ? null : t;
    }

    private static string? NormalizeUnit(string? unit)
    {
        unit = AsNullIfPlaceholder(unit);
        if (unit is null) return null;
        var u = unit.Trim().ToLowerInvariant();
        // chỉ chấp nhận mm/cm/in; nếu khác → null để fallback theo template
        return (u is "mm" or "cm" or "in") ? u : null;
    }

    private static string? NormalizeLayersCsv(string? csv)
    {
        csv = AsNullIfPlaceholder(csv);
        if (string.IsNullOrWhiteSpace(csv)) return null;

        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Where(p => !string.IsNullOrWhiteSpace(p))
                       .Distinct(StringComparer.OrdinalIgnoreCase);
        var joined = string.Join(",", parts);
        return string.IsNullOrWhiteSpace(joined) ? null : joined;
    }

    private static string ToHttps(string url)
        => string.IsNullOrWhiteSpace(url)
            ? url
            : url.Replace("http://", "https://", StringComparison.OrdinalIgnoreCase);
}

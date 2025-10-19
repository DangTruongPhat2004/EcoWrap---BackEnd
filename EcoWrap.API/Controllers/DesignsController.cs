using System.Net.Mime;
using System.Security.Claims;
using EcoWrap.BLL.IServices;
using EcoWrap.Models.Designs;
using EcoWrap.BLL.Validation;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EcoWrap.API.Controllers;

[ApiController]
[Route("designs")]
[Authorize] // yêu cầu đăng nhập cho toàn bộ endpoints
public class DesignsController : ControllerBase
{
    private readonly ICustomDesignsService _svc;
    private readonly IValidator<CreateDesignDto> _createValidator;
    private readonly IValidator<UpdateDesignDto> _updateValidator;

    public DesignsController(
        ICustomDesignsService svc,
        IValidator<CreateDesignDto> createValidator,
        IValidator<UpdateDesignDto> updateValidator)
    {
        _svc = svc;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    // ==== Helpers ====
    private long CurrentUserId
    {
        get
        {
            // Hỗ trợ nhiều kiểu claim: NameIdentifier / "id" / sub
            var idStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User.FindFirstValue("id")
                ?? User.FindFirstValue("sub");

            if (!long.TryParse(idStr, out var uid))
                throw new UnauthorizedAccessException("Invalid user id claim");
            return uid;
        }
    }

    private static string ETagFromRowVersion(byte[] rowVersion)
        => $"W/\"{Convert.ToBase64String(rowVersion)}\"";

    private static byte[]? ParseIfMatch(string? ifMatchHeader)
    {
        if (string.IsNullOrWhiteSpace(ifMatchHeader)) return null;
        var s = ifMatchHeader.Trim();
        if (s.StartsWith("W/\"", StringComparison.Ordinal)) s = s[3..];
        if (s.StartsWith('\"')) s = s[1..];
        if (s.EndsWith('\"')) s = s[..^1];
        try { return Convert.FromBase64String(s); } catch { return null; }
    }

    private ActionResult ValidationProblem(ValidationResult vr)
        => Problem(statusCode: 400, title: "validation_error",
            detail: string.Join("; ", vr.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")));

    // ==== GET /designs (list) ====
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? q = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var (items, total) = await _svc.GetListAsync(CurrentUserId, page, pageSize, q, status, ct);
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        return Ok(new
        {
            items,
            page,
            pageSize,
            total,
            totalPages,
            hasNext = page < totalPages
        });
    }

    // ==== GET /designs/{id} (detail) ====
    [HttpGet("{id:long}")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult> Get(long id, CancellationToken ct)
    {
        var got = await _svc.GetAsync(id, CurrentUserId, ct);
        if (got is null) return NotFound();

        var (dto, rv) = got.Value;
        var etag = ETagFromRowVersion(rv);

        // If-None-Match → 304
        var inm = Request.Headers.IfNoneMatch.FirstOrDefault();
        if (!string.IsNullOrEmpty(inm) && string.Equals(inm, etag, StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = etag;
        return Ok(dto);
    }

    // ==== POST /designs (create) ====
    [HttpPost]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<ActionResult> Create([FromBody] CreateDesignDto body, CancellationToken ct)
    {
        var vr = await _createValidator.ValidateAsync(body, ct);
        if (!vr.IsValid) return ValidationProblem(vr);

        var (dto, rv) = await _svc.CreateAsync(CurrentUserId, body, ct);
        Response.Headers.ETag = ETagFromRowVersion(rv);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    // ==== PUT /designs/{id} (update with If-Match) ====
    [HttpPut("{id:long}")]
    [Consumes("application/json")]
    public async Task<ActionResult> Update(
    long id,
    [FromBody] UpdateDesignDto body,
    [FromHeader(Name = "If-Match")] string? ifMatchHeader,   // 👈 thêm dòng này
    CancellationToken ct)
    {
        var vr = await _updateValidator.ValidateAsync(body, ct);
        if (!vr.IsValid) return ValidationProblem(vr);

        var ifMatch = ParseIfMatch(ifMatchHeader);
        if (ifMatch == null)
            return Problem(statusCode: 428, title: "precondition_required", detail: "If-Match header is required");

        try
        {
            var result = await _svc.UpdateAsync(id, CurrentUserId, ifMatch, body, ct);
            if (result is null) return NotFound();

            var (dto, rv) = result.Value;
            Response.Headers.ETag = ETagFromRowVersion(rv);
            return Ok(dto);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: 409, title: "etag_conflict", detail: "The design has been modified by someone else");
        }
    }

    // ==== DELETE /designs/{id} ====
    [HttpDelete("{id:long}")]
    public async Task<ActionResult> Delete(long id, [FromQuery] bool hard = false, CancellationToken ct = default)
    {
        var ok = await _svc.DeleteAsync(id, CurrentUserId, hard, ct);
        if (!ok) return NotFound();
        return NoContent();
    }
}

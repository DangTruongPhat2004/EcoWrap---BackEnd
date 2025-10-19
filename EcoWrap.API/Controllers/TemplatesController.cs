using EcoWrap.BLL.Services;
using EcoWrap.Models.Templates;
using Microsoft.AspNetCore.Mvc;

namespace EcoWrap.API.Controllers;

[ApiController]
[Route("templates")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _svc;
    private readonly ILogger<TemplatesController> _logger;

    public TemplatesController(ITemplateService svc, ILogger<TemplatesController> logger)
    {
        _svc = svc;
        _logger = logger;
    }

    // GET /templates?page=&pageSize=&search=&category=&sort=
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TemplateListItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] TemplateQuery query, CancellationToken ct)
    {
        if (query.Page < 1 || query.PageSize < 1 || query.PageSize > 100)
            return Problem(statusCode: 400, title: "Invalid pagination parameters");

        var result = await _svc.ListAsync(query, ct);

        // nhẹ nhàng set cache cho list (dev: 120s; prod bạn có thể 300–600s)
        Response.Headers.CacheControl = "public,max-age=120";
        return Ok(result);
    }

    // GET /templates/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TemplateDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Detail([FromRoute] int id, CancellationToken ct)
    {
        var data = await _svc.GetDetailAsync(id, ct);
        if (data is null) return NotFound();

        var (dto, etag) = data.Value;

        // ETag handling: 304 nếu client gửi If-None-Match khớp
        var inm = Request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(inm) && string.Equals(inm, etag, StringComparison.Ordinal))
            return StatusCode(StatusCodes.Status304NotModified);

        Response.Headers.ETag = etag;
        Response.Headers.CacheControl = "public,max-age=300";
        return Ok(dto);
    }
}

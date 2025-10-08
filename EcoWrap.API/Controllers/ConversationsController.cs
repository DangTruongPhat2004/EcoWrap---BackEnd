using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.Models.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcoWrap.API.Controllers
{
    [ApiController]
    [Route("chats/conversations")]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly IConversationService _svc;
        public ConversationsController(IConversationService svc) => _svc = svc;

        int CallerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ConversationCreateRequest req, CancellationToken ct)
        {
            var dto = await _svc.CreateOrGetAsync(req.sellerId, req.customerId, CallerId, ct);
            return Ok(dto);
        }


        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string? cursor, [FromQuery] int limit = 20, CancellationToken ct = default)
        {
            var dto = await _svc.ListMineAsync(CallerId, cursor, Math.Clamp(limit, 1, 50), ct);
            return Ok(dto);
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Get(long id, CancellationToken ct)
        {
            var dto = await _svc.GetAsync(id, CallerId, ct);
            if (dto is null) return NotFound();
            return Ok(dto);
        }
    }
}

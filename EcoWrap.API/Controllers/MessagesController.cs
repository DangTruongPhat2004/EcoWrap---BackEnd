using System;
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
    [Route("chats/conversations/{conversationId:long}/messages")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _svc;
        public MessagesController(IMessageService svc) => _svc = svc;

        int CallerId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

        [HttpGet]
        public async Task<IActionResult> List(long conversationId, [FromQuery] string? cursor, [FromQuery] int limit = 30, CancellationToken ct = default)
        {
            var dto = await _svc.ListAsync(conversationId, cursor, Math.Clamp(limit, 1, 50), CallerId, ct);
            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Send(long conversationId, [FromBody] SendMessageRequest req, CancellationToken ct)
        {
            var dto = await _svc.SendAsync(conversationId, req, CallerId, ct);
            return CreatedAtAction(nameof(List), new { conversationId }, dto);
        }

        [HttpPost("read")]
        public async Task<IActionResult> MarkRead(long conversationId, [FromBody] ReadRequest req, CancellationToken ct)
        {
            var ok = await _svc.MarkReadAsync(conversationId, req.messageId, CallerId, ct);
            return Ok(new { success = ok });
        }
    }
}

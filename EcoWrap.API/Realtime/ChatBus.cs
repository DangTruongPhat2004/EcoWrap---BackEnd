using System;
using System.Threading;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.Models.Chat;
using Microsoft.AspNetCore.SignalR;

namespace EcoWrap.API.Realtime
{
    public class ChatBus : IChatBus
    {
        private readonly IHubContext<ChatHub> _hub;
        public ChatBus(IHubContext<ChatHub> hub) => _hub = hub;

        public Task PublishMessageCreatedAsync(long conversationId, MessageItemDto message, CancellationToken ct) =>
            _hub.Clients.Group($"conv:{conversationId}")
                .SendAsync("MessageCreated", new { conversationId, message }, ct);

        public Task PublishReadReceiptAsync(long conversationId, long messageId, Guid userId, DateTime readAt, CancellationToken ct) =>
            _hub.Clients.Group($"conv:{conversationId}")
                .SendAsync("ReadReceipt", new { conversationId, messageId, userId, readAt }, ct);
    }
}

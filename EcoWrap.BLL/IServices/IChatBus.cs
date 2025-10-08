using System;
using System.Threading;
using System.Threading.Tasks;
using EcoWrap.Models.Chat;

namespace EcoWrap.BLL.IServices
{
    /// Realtime bus abstraction (để BLL không phụ thuộc SignalR)
    public interface IChatBus
    {
        Task PublishMessageCreatedAsync(long conversationId, MessageItemDto message, CancellationToken ct);
        Task PublishReadReceiptAsync(long conversationId, long messageId, Guid userId, DateTime readAt, CancellationToken ct);
    }

    /// No-op fallback (nếu môi trường chưa có SignalR)
    public sealed class NoopChatBus : IChatBus
    {
        public Task PublishMessageCreatedAsync(long _, MessageItemDto __, CancellationToken ___) => Task.CompletedTask;
        public Task PublishReadReceiptAsync(long _, long __, Guid ___, DateTime ____, CancellationToken _____) => Task.CompletedTask;
    }
}

using System.Threading;
using System.Threading.Tasks;
using EcoWrap.Models.Chat;

namespace EcoWrap.BLL.IServices
{
    public interface IMessageService
    {
        Task<PagedMessagesDto> ListAsync(long conversationId, string? cursor, int limit, int callerId, CancellationToken ct);
        Task<MessageItemDto> SendAsync(long conversationId, SendMessageRequest req, int callerId, CancellationToken ct);
        Task<bool> MarkReadAsync(long conversationId, long messageId, int callerId, CancellationToken ct);
    }

}

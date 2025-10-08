using System;
using System.Threading;
using System.Threading.Tasks;
using EcoWrap.Models.Chat;

namespace EcoWrap.BLL.IServices
{
    public interface IConversationService
    {
        Task<ConversationDetailDto> CreateOrGetAsync(int sellerId, int customerId, int callerId, CancellationToken ct);
        Task<PagedConversationsDto> ListMineAsync(int callerId, string? cursor, int limit, CancellationToken ct);
        Task<ConversationDetailDto> GetAsync(long id, int callerId, CancellationToken ct);
    }

}

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.Repositories.Chat
{
    public interface IConversationRepository
    {
        Task<Conversation?> FindPairAsync(int sellerId, int customerId, CancellationToken ct);
        Task<Conversation> AddAsync(Conversation c, CancellationToken ct);
        Task<Conversation?> GetAsync(long id, CancellationToken ct);
        Task<bool> IsParticipantAsync(long conversationId, int userId, CancellationToken ct);
        Task<List<Conversation>> ListMineAsync(int userId, DateTime? before, long? beforeId, int take, CancellationToken ct);
        Task SaveChangesAsync(CancellationToken ct);
    }
}

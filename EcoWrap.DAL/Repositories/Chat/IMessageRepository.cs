using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoWrap.DAL.Entities;

namespace EcoWrap.DAL.Repositories.Chat
{
    public interface IMessageRepository
    {
        Task<Message> AddAsync(Message m, CancellationToken ct);

        // Đổi về Task<List<Message>>
        Task<List<Message>> ListAsync(long conversationId, DateTime? before, long? beforeId, int take, CancellationToken ct);
        Task<Message?> GetAsync(long id, CancellationToken ct);
        Task UpsertReceiptAsync(long messageId, int userId, bool read, CancellationToken ct);

        Task SaveChangesAsync(CancellationToken ct);
    }
}

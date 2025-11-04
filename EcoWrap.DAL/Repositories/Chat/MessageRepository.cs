using EcoWrap.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EcoWrap.DAL.Repositories.Chat
{
    public class MessageRepository : IMessageRepository
    {
        private readonly EcoWrapDbContext _db;
        public MessageRepository(EcoWrapDbContext db) => _db = db;

        public async Task<Message> AddAsync(Message m, CancellationToken ct)
        {
            _db.Messages.Add(m);
            await _db.SaveChangesAsync(ct);
            return m;
        }

        public Task<List<Message>> ListAsync(
            long conversationId, DateTime? before, long? beforeId, int take, CancellationToken ct)
        {
            IQueryable<Message> q = _db.Messages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId);

            if (before.HasValue && beforeId.HasValue)
            {
                q = q.Where(x =>
                    x.CreatedAt < before ||
                    (x.CreatedAt == before && x.Id < beforeId));
            }

            q = q
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id);

            return q.Take(take).ToListAsync(ct);
        }

        public Task<Message?> GetAsync(long id, CancellationToken ct) =>
            _db.Messages.FirstOrDefaultAsync(x => x.Id == id, ct);

        // ✅ đổi Guid -> int để khớp IConversation/IMessage & schema Users(UserId int)
        public async Task UpsertReceiptAsync(long messageId, int userId, bool read, CancellationToken ct)
        {
            var r = await _db.MessageReceipts.FindAsync(new object[] { messageId, userId }, ct);
            if (r == null)
            {
                r = new MessageReceipt
                {
                    MessageId = messageId,
                    UserId = userId,                // int
                    DeliveredAt = DateTime.UtcNow.AddHours(7)
                };
                _db.MessageReceipts.Add(r);
            }

            if (read)
                r.ReadAt = DateTime.UtcNow.AddHours(7);

            await _db.SaveChangesAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}

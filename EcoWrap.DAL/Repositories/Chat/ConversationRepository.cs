using EcoWrap.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EcoWrap.DAL.Repositories.Chat
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly EcoWrapDbContext _db;
        public ConversationRepository(EcoWrapDbContext db) => _db = db;

        // ✅ int thay vì Guid
        public Task<Conversation?> FindPairAsync(int sellerId, int customerId, CancellationToken ct) =>
            _db.Conversations
               .Include(c => c.Participants)
               .Where(c => c.Participants.Count == 2)
               .Where(c => c.Participants.Any(p => p.UserId == sellerId && p.Role == ChatRole.Seller))
               .Where(c => c.Participants.Any(p => p.UserId == customerId && p.Role == ChatRole.Customer))
               .FirstOrDefaultAsync(ct);

        public async Task<Conversation> AddAsync(Conversation c, CancellationToken ct)
        {
            _db.Conversations.Add(c);
            await _db.SaveChangesAsync(ct);
            return c;
        }

        public Task<Conversation?> GetAsync(long id, CancellationToken ct) =>
            _db.Conversations.Include(c => c.Participants).FirstOrDefaultAsync(c => c.Id == id, ct);

        // ✅ int thay vì Guid
        public Task<bool> IsParticipantAsync(long conversationId, int userId, CancellationToken ct) =>
            _db.Conversations.AnyAsync(c => c.Id == conversationId && c.Participants.Any(p => p.UserId == userId), ct);

        // ✅ int thay vì Guid
        public Task<List<Conversation>> ListMineAsync(int userId, DateTime? before, long? beforeId, int take, CancellationToken ct)
        {
            IQueryable<Conversation> q = _db.Conversations
                .AsNoTracking()
                .Where(c => c.Participants.Any(p => p.UserId == userId));

            if (before.HasValue && beforeId.HasValue)
            {
                q = q.Where(c =>
                    c.LastMessageAt < before ||
                    (c.LastMessageAt == before && c.Id < beforeId));
            }

            q = q
                .OrderByDescending(c => c.LastMessageAt)
                .ThenByDescending(c => c.Id);

            return q.Take(take).ToListAsync(ct);
        }

        public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
    }
}

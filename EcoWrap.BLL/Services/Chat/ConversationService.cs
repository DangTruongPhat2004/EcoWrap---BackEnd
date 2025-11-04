using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.BLL.Mapping;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Chat;
using Microsoft.EntityFrameworkCore;
using ChatRole = EcoWrap.DAL.Entities.ChatRole;


namespace EcoWrap.BLL.Services
{
    public class ConversationService : IConversationService
    {
        private readonly EcoWrapDbContext _db;
        public ConversationService(EcoWrapDbContext db) => _db = db;

        public async Task<ConversationDetailDto> CreateOrGetAsync(int sellerId, int customerId, int callerId, CancellationToken ct)
        {
            if (callerId != sellerId && callerId != customerId)
                throw new UnauthorizedAccessException("Caller must be one of participants.");

            var convo = await _db.Conversations
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c =>
                    c.Participants.Any(p => p.UserId == sellerId && p.Role == ChatRole.Seller) &&
                    c.Participants.Any(p => p.UserId == customerId && p.Role == ChatRole.Customer),
                    ct);

            if (convo is null)
            {
                convo = new Conversation
                {
                    CreatedAt = DateTime.UtcNow.AddHours(7),
                    UpdatedAt = DateTime.UtcNow.AddHours(7),
                    IsClosed = false,
                    Participants =
                    {
                        new ConversationParticipant { UserId = sellerId, Role = ChatRole.Seller },
                        new ConversationParticipant { UserId = customerId, Role = ChatRole.Customer }
                    }
                };
                _db.Conversations.Add(convo);
                await _db.SaveChangesAsync(ct);
            }

            var dto = await _db.Conversations.AsNoTracking()
                .Where(c => c.Id == convo.Id)
                .SelectDetailDto()
                .FirstAsync(ct);

            return dto;
        }

        public async Task<PagedConversationsDto> ListMineAsync(int callerId, string? cursor, int limit, CancellationToken ct)
        {
            var q = _db.Conversations.AsNoTracking()
                .Where(c => c.Participants.Any(p => p.UserId == callerId));

            if (long.TryParse(cursor, out var curId) && curId > 0)
                q = q.Where(c => c.Id < curId);

            q = q.OrderByDescending(c => c.LastMessageAt.HasValue)
                 .ThenByDescending(c => c.LastMessageAt)
                 .ThenByDescending(c => c.Id);

            var items = await q.Take(limit).SelectItemDto().ToListAsync(ct);
            var nextCursor = (items.Count == limit) ? items.Last().id.ToString() : null;

            return new PagedConversationsDto(items, nextCursor);
        }

        public async Task<ConversationDetailDto?> GetAsync(long conversationId, int callerId, CancellationToken ct)
        {
            var dto = await _db.Conversations.AsNoTracking()
                .Where(c => c.Id == conversationId && c.Participants.Any(p => p.UserId == callerId))
                .SelectDetailDto()
                .FirstOrDefaultAsync(ct);

            return dto;
        }
    }
}

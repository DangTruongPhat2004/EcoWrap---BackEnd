// EcoWrap.BLL/Mapping/ConversationMapping.cs
using System.Linq;
using EcoWrap.DAL.Entities;
using EcoWrap.Models.Chat;
// Bỏ using Microsoft.EntityFrameworkCore; vì không cần EF.Property nữa

namespace EcoWrap.BLL.Mapping
{
    public static class ConversationMapping
    {
        public static IQueryable<ConversationItemDto> SelectItemDto(this IQueryable<Conversation> q)
        {
            return q.Select(c => new ConversationItemDto(
                c.Id,
                c.LastMessageAt,
                c.LastMessagePreview,
                c.IsClosed,
                c.Participants
                    .Select(p => new ParticipantDto(
                        p.UserId, // Không cần cast (int) nếu UserId trong ParticipantDto là kiểu int
                        p.Role.ToString() // ✅ SỬA Ở ĐÂY
                    ))
                    .ToList()
            ));
        }

        public static IQueryable<ConversationDetailDto> SelectDetailDto(this IQueryable<Conversation> q)
        {
            return q.Select(c => new ConversationDetailDto(
                c.Id,
                c.IsClosed,
                c.CreatedAt,
                c.LastMessageAt,
                c.LastMessagePreview,
                c.Participants
                    .Select(p => new ParticipantDto(
                        p.UserId,
                        p.Role.ToString() // ✅ SỬA Ở ĐÂY
                    ))
                    .ToList()
            ));
        }
    }
}
using System;

namespace EcoWrap.DAL.Entities
{
   

    public class ConversationParticipant
    {
        public long ConversationId { get; set; }
        public int UserId { get; set; }
        public ChatRole Role { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public Conversation Conversation { get; set; } = null!;
        public User User { get; set; } = null!; // đã có User trong dự án của bạn
    }
}

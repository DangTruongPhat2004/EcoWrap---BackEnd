using System;
using System.Collections.Generic;

namespace EcoWrap.DAL.Entities
{
    public class Conversation
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? LastMessagePreview { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public bool IsClosed { get; set; } = false;

        public ICollection<ConversationParticipant> Participants { get; set; } = new List<ConversationParticipant>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}

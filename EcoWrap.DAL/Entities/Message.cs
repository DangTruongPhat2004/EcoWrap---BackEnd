using System;
using System.Collections.Generic;

namespace EcoWrap.DAL.Entities
{
    public enum ContentType { text, image, file, system }

    public class Message
    {
        public long Id { get; set; }
        public long ConversationId { get; set; }
        public int SenderId { get; set; }
        public string? Body { get; set; }
        public ContentType ContentType { get; set; } = ContentType.text;
        public string? AttachmentUrl { get; set; }
        public string? AttachmentName { get; set; }
        public int? AttachmentSize { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(7);

        public Conversation Conversation { get; set; } = null!;
        public ICollection<MessageReceipt> Receipts { get; set; } = new List<MessageReceipt>();
    }
}

using System;

namespace EcoWrap.DAL.Entities
{
    public class MessageReceipt
    {
        public long MessageId { get; set; }
        public int UserId { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? ReadAt { get; set; }

        public Message Message { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}

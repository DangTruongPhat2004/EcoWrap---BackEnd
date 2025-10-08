using System;
using System.Collections.Generic; // KeyNotFoundException
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoWrap.BLL.IServices;
using EcoWrap.DAL.Entities;
using EcoWrap.DAL.Repositories.Chat;
using EcoWrap.Models.Chat;
using ChatContentType = EcoWrap.DAL.Entities.ContentType;

namespace EcoWrap.BLL.Services.Chat
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _msgRepo;
        private readonly IConversationRepository _convRepo;
        private readonly IChatBus _bus;

        public MessageService(IMessageRepository msgRepo, IConversationRepository convRepo, IChatBus bus)
        {
            _msgRepo = msgRepo; _convRepo = convRepo; _bus = bus;
        }

        // ✅ callerId = int
        public async Task<PagedMessagesDto> ListAsync(long conversationId, string? cursor, int limit, int callerId, CancellationToken ct)
        {
            if (!await _convRepo.IsParticipantAsync(conversationId, callerId, ct))
                throw new UnauthorizedAccessException();

            (DateTime? before, long? beforeId) = DecodeCursor(cursor);

            var rows = await _msgRepo.ListAsync(conversationId, before, beforeId, limit + 1, ct);

            string? next = null;
            if (rows.Count > limit)
            {
                var last = rows[limit - 1];
                next = EncodeCursor(last.CreatedAt, last.Id);
                rows = rows.Take(limit).ToList();
            }

            var items = rows.Select(Map).ToList();
            return new(items, next);
        }

        // ✅ callerId = int
        public async Task<MessageItemDto> SendAsync(long conversationId, SendMessageRequest req, int callerId, CancellationToken ct)
        {
            if (!await _convRepo.IsParticipantAsync(conversationId, callerId, ct))
                throw new UnauthorizedAccessException();

            var m = new Message
            {
                ConversationId = conversationId,
                SenderId = callerId,                 // ✅ int
                Body = req.body,
                ContentType = req.contentType,       // ChatContentType == DAL.Entities.ContentType
                AttachmentUrl = req.attachmentUrl,
                AttachmentName = req.attachmentName,
                AttachmentSize = req.attachmentSize
            };
            await _msgRepo.AddAsync(m, ct);

            // update conversation summary
            var conv = await _convRepo.GetAsync(conversationId, ct) ?? throw new KeyNotFoundException();
            conv.LastMessageAt = m.CreatedAt;
            conv.LastMessagePreview =
                m.ContentType == ChatContentType.text
                ? (m.Body ?? string.Empty)[..Math.Min(200, m.Body?.Length ?? 0)]
                : $"[{m.ContentType}]";
            conv.UpdatedAt = DateTime.UtcNow;
            await _convRepo.SaveChangesAsync(ct);

            var dto = Map(m);

            // broadcast
            await _bus.PublishMessageCreatedAsync(conversationId, dto, ct);

            return dto;
        }

        // ✅ callerId = int
        public async Task<bool> MarkReadAsync(long conversationId, long messageId, int callerId, CancellationToken ct)
        {
            if (!await _convRepo.IsParticipantAsync(conversationId, callerId, ct))
                throw new UnauthorizedAccessException();

            var msg = await _msgRepo.GetAsync(messageId, ct);
            if (msg == null || msg.ConversationId != conversationId)
                throw new KeyNotFoundException();

            await _msgRepo.UpsertReceiptAsync(messageId, callerId, read: true, ct); // ✅ int

            // Convert int callerId to Guid before passing to PublishReadReceiptAsync
            Guid callerGuid = new Guid(callerId, 0, 0, new byte[8]);
            await _bus.PublishReadReceiptAsync(conversationId, messageId, callerGuid, DateTime.UtcNow, ct);
            return true;
        }

        private static MessageItemDto Map(Message m) =>
            new(m.Id, m.SenderId, m.Body, m.ContentType, m.AttachmentUrl, m.AttachmentName, m.AttachmentSize, m.CreatedAt);

        private static string EncodeCursor(DateTime dt, long id)
        {
            var raw = $"{dt.Ticks}_{id}";
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(raw));
        }
        private static (DateTime?, long?) DecodeCursor(string? cursor)
        {
            if (string.IsNullOrWhiteSpace(cursor)) return (null, null);
            try
            {
                var raw = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
                var parts = raw.Split('_');
                return (new DateTime(long.Parse(parts[0]), DateTimeKind.Utc), long.Parse(parts[1]));
            }
            catch { return (null, null); }
        }
    }
}

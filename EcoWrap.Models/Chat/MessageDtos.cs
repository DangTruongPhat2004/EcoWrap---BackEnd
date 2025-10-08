using System;
using System.Collections.Generic;
using EcoWrap.DAL.Entities;

namespace EcoWrap.Models.Chat
{
    public record SendMessageRequest(string? body, ContentType contentType = ContentType.text, string? clientId = null,
                                     string? attachmentUrl = null, string? attachmentName = null, int? attachmentSize = null);

    public record MessageItemDto(long id, int senderId, string? body, ContentType contentType,
                                 string? attachmentUrl, string? attachmentName, int? attachmentSize,
                                 DateTime createdAt);

    public record PagedMessagesDto(IEnumerable<MessageItemDto> items, string? nextCursor);
    public record ReadRequest(long messageId);
}

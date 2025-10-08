using System;
using System.Collections.Generic;
using EcoWrap.DAL.Entities;

namespace EcoWrap.Models.Chat
{
    public record ConversationCreateRequest(int sellerId, int customerId);

    // ✅ Thêm participants vào item để sidebar có tên/ảnh đối phương
    public record ConversationItemDto(
        long id,
        DateTime? lastMessageAt,
        string? lastMessagePreview,
        bool isClosed,
        IEnumerable<ParticipantDto> participants
    );

    public record ParticipantDto(int userId, string role);

    public record ConversationDetailDto(
        long id,
        bool isClosed,
        DateTime createdAt,
        DateTime? lastMessageAt,
        string? lastMessagePreview,
        IEnumerable<ParticipantDto> participants
    );

    public record PagedConversationsDto(IEnumerable<ConversationItemDto> items, string? nextCursor);
}

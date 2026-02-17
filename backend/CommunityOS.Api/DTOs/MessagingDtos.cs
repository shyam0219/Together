namespace CommunityOS.Api.DTOs;

public sealed record ConversationListItemDto(
    Guid ConversationId,
    IReadOnlyList<Guid> ParticipantUserIds,
    string? LastMessagePreview,
    DateTimeOffset? LastMessageAt,
    int UnreadCount
);

public sealed record MessageDto(
    Guid MessageId,
    Guid ConversationId,
    Guid SenderId,
    string SenderName,
    string BodyText,
    DateTimeOffset SentAt
);

public sealed record StartConversationRequest(Guid OtherUserId);

public sealed record SendMessageRequest(string BodyText);

public sealed record MarkReadRequest(DateTimeOffset? ReadAt);

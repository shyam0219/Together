namespace CommunityOS.Api.DTOs;

public sealed record NotificationDto(
    Guid NotificationId,
    string Type,
    string PayloadJson,
    bool IsRead,
    DateTimeOffset CreatedAt
);

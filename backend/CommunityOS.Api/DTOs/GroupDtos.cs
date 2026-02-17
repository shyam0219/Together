using CommunityOS.Domain.Entities;

namespace CommunityOS.Api.DTOs;

public sealed record GroupDto(
    Guid GroupId,
    string Name,
    string? Description,
    string Visibility,
    Guid CreatedById,
    DateTimeOffset CreatedAt,
    int MemberCount,
    bool IsMember
);

public sealed record CreateGroupRequest(string Name, string? Description, string Visibility);

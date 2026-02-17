using CommunityOS.Domain.Entities;

namespace CommunityOS.Api.DTOs;

public sealed record MemberListItemDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string? City,
    string? Bio,
    string? AvatarUrl,
    string Role,
    string Status
);

public static class MemberDtoMapper
{
    public static MemberListItemDto ToListItem(User u) => new(
        u.UserId,
        u.Email,
        u.FirstName,
        u.LastName,
        u.City,
        u.Bio,
        u.AvatarUrl,
        u.Role.ToString(),
        u.Status.ToString()
    );
}

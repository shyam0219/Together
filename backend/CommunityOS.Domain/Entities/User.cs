namespace CommunityOS.Domain.Entities;

public sealed class User : BaseEntity
{
    public Guid UserId { get; set; }

    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;

    public string? City { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;
    public UserRole Role { get; set; } = UserRole.Member;
}

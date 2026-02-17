namespace CommunityOS.Domain.Entities;

public sealed class GroupMember : BaseEntity
{
    public Guid GroupMemberId { get; set; }

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public GroupMemberRole Role { get; set; } = GroupMemberRole.Member;

    public DateTimeOffset JoinedAt { get; set; }
}

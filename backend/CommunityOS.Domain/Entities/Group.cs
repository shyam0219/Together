namespace CommunityOS.Domain.Entities;

public sealed class Group : BaseEntity
{
    public Guid GroupId { get; set; }

    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    public GroupVisibility Visibility { get; set; } = GroupVisibility.Public;

    public Guid CreatedById { get; set; }
    public User CreatedBy { get; set; } = null!;

    public List<GroupMember> Members { get; set; } = new();
}

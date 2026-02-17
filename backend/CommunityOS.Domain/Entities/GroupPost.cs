namespace CommunityOS.Domain.Entities;

/// <summary>
/// Links a post to a group (optional Phase 1A). Used for group privacy enforcement.
/// </summary>
public sealed class GroupPost : BaseEntity
{
    public Guid GroupPostId { get; set; }

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
}

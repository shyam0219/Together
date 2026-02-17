namespace CommunityOS.Domain.Entities;

public sealed class Bookmark : BaseEntity
{
    public Guid BookmarkId { get; set; }

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

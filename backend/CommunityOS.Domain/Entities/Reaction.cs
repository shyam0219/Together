namespace CommunityOS.Domain.Entities;

public sealed class Reaction : BaseEntity
{
    public Guid ReactionId { get; set; }

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public ReactionType Type { get; set; } = ReactionType.Like;
}

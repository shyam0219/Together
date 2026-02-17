namespace CommunityOS.Domain.Entities;

public sealed class Post : SoftDeletableEntity
{
    public Guid PostId { get; set; }

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public string BodyText { get; set; } = null!;

    public string? LinkUrl { get; set; }
    public string? LinkTitle { get; set; }
    public string? LinkDescription { get; set; }
    public string? LinkImageUrl { get; set; }

    public bool CommentingEnabled { get; set; } = true;

    public PostStatus Status { get; set; } = PostStatus.Active;

    public List<PostImage> Images { get; set; } = new();
}

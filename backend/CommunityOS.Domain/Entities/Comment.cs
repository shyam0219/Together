namespace CommunityOS.Domain.Entities;

public sealed class Comment : SoftDeletableEntity
{
    public Guid CommentId { get; set; }

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;

    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }

    public string Text { get; set; } = null!;

    public List<Comment> Replies { get; set; } = new();
}

namespace CommunityOS.Domain.Entities;

public sealed class PostImage : BaseEntity
{
    public Guid PostImageId { get; set; }

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public string Url { get; set; } = null!;

    public int SortOrder { get; set; }
}

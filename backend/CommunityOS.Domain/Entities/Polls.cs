namespace CommunityOS.Domain.Entities;

public sealed class Poll : BaseEntity
{
    public Guid PollId { get; set; }

    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;

    public string Question { get; set; } = null!;

    public List<PollOption> Options { get; set; } = new();
}

public sealed class PollOption : BaseEntity
{
    public Guid PollOptionId { get; set; }

    public Guid PollId { get; set; }
    public Poll Poll { get; set; } = null!;

    public string Text { get; set; } = null!;
    public int SortOrder { get; set; }

    public List<PollVote> Votes { get; set; } = new();
}

public sealed class PollVote : BaseEntity
{
    public Guid PollVoteId { get; set; }

    public Guid PollId { get; set; }
    public Poll Poll { get; set; } = null!;

    public Guid PollOptionId { get; set; }
    public PollOption Option { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

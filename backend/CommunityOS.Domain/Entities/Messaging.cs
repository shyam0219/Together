namespace CommunityOS.Domain.Entities;

public sealed class Conversation : BaseEntity
{
    public Guid ConversationId { get; set; }

    // 1:1 canonical participants for uniqueness (optional for future group chat)
    public Guid? DirectUserAId { get; set; }
    public Guid? DirectUserBId { get; set; }

    public List<ConversationParticipant> Participants { get; set; } = new();
    public List<Message> Messages { get; set; } = new();
}

public sealed class ConversationParticipant : BaseEntity
{
    public Guid ConversationParticipantId { get; set; }

    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public DateTimeOffset JoinedAt { get; set; }

    public DateTimeOffset? LastReadAt { get; set; }
}

public sealed class Message : BaseEntity
{
    public Guid MessageId { get; set; }

    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;

    public string BodyText { get; set; } = null!;

    public DateTimeOffset SentAt { get; set; }
}

namespace CommunityOS.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public Guid NotificationId { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public NotificationType Type { get; set; }

    public string PayloadJson { get; set; } = "{}";

    public bool IsRead { get; set; } = false;
}

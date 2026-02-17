using System.Text.Json;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;

namespace CommunityOS.Api.Services;

public interface INotificationService
{
    Task CreateMentionAsync(Guid mentionedUserId, Guid actorUserId, string targetType, Guid targetId, CancellationToken ct = default);
}

public sealed class NotificationService : INotificationService
{
    private readonly AppDbContext _db;

    public NotificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task CreateMentionAsync(Guid mentionedUserId, Guid actorUserId, string targetType, Guid targetId, CancellationToken ct = default)
    {
        var payload = new
        {
            actorUserId,
            targetType,
            targetId
        };

        var n = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = mentionedUserId,
            Type = NotificationType.Mention,
            PayloadJson = JsonSerializer.Serialize(payload),
            IsRead = false
        };

        _db.Notifications.Add(n);
        await _db.SaveChangesAsync(ct);
    }
}

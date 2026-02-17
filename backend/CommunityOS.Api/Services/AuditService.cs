using System.Text.Json;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;

namespace CommunityOS.Api.Services;

public interface IAuditService
{
    Task LogAsync(Guid actorId, string actionType, string targetType, Guid targetId, object? metadata = null, CancellationToken ct = default);
}

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(Guid actorId, string actionType, string targetType, Guid targetId, object? metadata = null, CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            AuditLogId = Guid.NewGuid(),
            ActorId = actorId,
            ActionType = actionType,
            TargetType = targetType,
            TargetId = targetId,
            MetadataJson = metadata is null ? "{}" : JsonSerializer.Serialize(metadata)
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}

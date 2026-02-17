namespace CommunityOS.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public Guid AuditLogId { get; set; }

    public Guid ActorId { get; set; }
    public User Actor { get; set; } = null!;

    public string ActionType { get; set; } = null!;
    public string TargetType { get; set; } = null!;

    public Guid TargetId { get; set; }

    public string MetadataJson { get; set; } = "{}";
}

namespace CommunityOS.Domain.Entities;

public abstract class BaseEntity
{
    public Guid TenantId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

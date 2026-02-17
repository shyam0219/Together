namespace CommunityOS.Domain.Entities;

public abstract class SoftDeletableEntity : BaseEntity
{
    public DateTimeOffset? SoftDeletedAt { get; set; }
}

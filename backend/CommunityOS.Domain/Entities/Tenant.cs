namespace CommunityOS.Domain.Entities;

public sealed class Tenant
{
    public Guid TenantId { get; set; }

    public string Code { get; set; } = null!; // SE / IT

    public string Name { get; set; } = null!;
}

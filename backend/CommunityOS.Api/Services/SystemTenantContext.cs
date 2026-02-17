using CommunityOS.Infrastructure.Data;

namespace CommunityOS.Api.Services;

/// <summary>
/// Used for startup tasks (migrations/seed) where no HTTP request/JWT exists.
/// </summary>
public sealed class SystemTenantContext : ITenantContext
{
    public Guid CurrentTenantId => Guid.Empty;
    public bool HasTenant => false;
    public bool IsPlatformOwner => true;
}

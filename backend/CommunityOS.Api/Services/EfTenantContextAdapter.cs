using CommunityOS.Infrastructure.Data;

namespace CommunityOS.Api.Services;

public sealed class EfTenantContextAdapter : ITenantContext
{
    private readonly ITenantProvider _tenantProvider;

    public EfTenantContextAdapter(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public Guid CurrentTenantId => _tenantProvider.CurrentTenantId;
    public bool HasTenant => _tenantProvider.HasTenant;
    public bool IsPlatformOwner => _tenantProvider.IsPlatformOwner;
}

namespace CommunityOS.Api.Services;

public interface ITenantProvider
{
    Guid CurrentTenantId { get; }
    bool HasTenant { get; }
    bool IsPlatformOwner { get; }

    void Set(Guid tenantId, bool isPlatformOwner);
}

public sealed class TenantProvider : ITenantProvider
{
    private Guid? _tenantId;
    private bool _isPlatformOwner;

    public Guid CurrentTenantId => _tenantId ?? throw new InvalidOperationException("Tenant is not set for current request.");

    public bool HasTenant => _tenantId.HasValue;

    public bool IsPlatformOwner => _isPlatformOwner;

    public void Set(Guid tenantId, bool isPlatformOwner)
    {
        _tenantId = tenantId;
        _isPlatformOwner = isPlatformOwner;
    }
}

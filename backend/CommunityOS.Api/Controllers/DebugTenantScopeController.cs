using CommunityOS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/debug")]
public sealed class DebugTenantScopeController : ControllerBase
{
    [HttpGet("tenant")]
    [Authorize]
    public ActionResult GetTenant([FromServices] ITenantProvider tenantProvider)
    {
        return Ok(new
        {
            hasTenant = tenantProvider.HasTenant,
            tenantId = tenantProvider.HasTenant ? tenantProvider.CurrentTenantId : (Guid?)null,
            isPlatformOwner = tenantProvider.IsPlatformOwner
        });
    }
}

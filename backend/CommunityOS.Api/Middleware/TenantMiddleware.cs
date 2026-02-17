using System.Security.Claims;
using CommunityOS.Domain.Entities;
using CommunityOS.Api.Services;

namespace CommunityOS.Api.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        // Allow health checks without auth.
        var path = context.Request.Path.Value ?? string.Empty;
        if (path == "/" || path.StartsWith("/api/health"))
        {
            await _next(context);
            return;
        }

        // Require authenticated user
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "unauthorized" });
            return;
        }

        var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value
                            ?? context.User.FindFirst("TenantId")?.Value;

        if (string.IsNullOrWhiteSpace(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId) || tenantId == Guid.Empty)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "missing_or_invalid_tenant_id" });
            return;
        }

        var roleStr = context.User.FindFirst(ClaimTypes.Role)?.Value
                      ?? context.User.FindFirst("role")?.Value
                      ?? context.User.FindFirst("Role")?.Value;

        var isPlatformOwner = string.Equals(roleStr, UserRole.PlatformOwner.ToString(), StringComparison.OrdinalIgnoreCase);

        tenantProvider.Set(tenantId, isPlatformOwner);

        await _next(context);
    }
}

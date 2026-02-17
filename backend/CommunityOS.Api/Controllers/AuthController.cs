using CommunityOS.Api.Services;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    public sealed record RegisterRequest(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        string TenantCode
    );

    public sealed record LoginRequest(string Email, string Password, string TenantCode);

    public sealed record AuthResponse(string Token);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(
        [FromBody] RegisterRequest req,
        [FromServices] AppDbContext db,
        [FromServices] IPasswordHasher hasher,
        [FromServices] IJwtTokenService jwt,
        [FromServices] ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.TenantCode))
            return BadRequest(new { error = "missing_fields" });

        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Code == req.TenantCode.Trim().ToUpperInvariant(), ct);
        if (tenant is null) return BadRequest(new { error = "invalid_tenant" });

        // Set current tenant for global filters + SaveChanges auto TenantId
        tenantProvider.Set(tenant.TenantId, isPlatformOwner: false);

        var emailNorm = req.Email.Trim().ToLowerInvariant();
        var exists = await db.Users.AnyAsync(u => u.Email == emailNorm, ct);
        if (exists) return Conflict(new { error = "email_already_exists" });

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Email = emailNorm,
            PasswordHash = hasher.Hash(req.Password),
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Role = UserRole.Member,
            Status = UserStatus.Active,
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        var token = jwt.CreateToken(user);
        return Ok(new AuthResponse(token));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest req,
        [FromServices] AppDbContext db,
        [FromServices] IPasswordHasher hasher,
        [FromServices] IJwtTokenService jwt,
        [FromServices] ITenantProvider tenantProvider,
        CancellationToken ct)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Code == req.TenantCode.Trim().ToUpperInvariant(), ct);
        if (tenant is null) return BadRequest(new { error = "invalid_tenant" });

        tenantProvider.Set(tenant.TenantId, isPlatformOwner: false);

        var emailNorm = req.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailNorm, ct);
        if (user is null) return Unauthorized(new { error = "invalid_credentials" });

        if (user.Status is UserStatus.Suspended or UserStatus.Banned)
            return Forbid();

        if (!hasher.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { error = "invalid_credentials" });

        var token = jwt.CreateToken(user);
        return Ok(new AuthResponse(token));
    }
}

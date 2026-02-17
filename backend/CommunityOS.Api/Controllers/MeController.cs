using System.Security.Claims;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class MeController : ControllerBase
{
    public sealed record MeResponse(
        Guid UserId,
        Guid TenantId,
        string Email,
        string FirstName,
        string LastName,
        string Role,
        string Status
    );

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<MeResponse>> Me([FromServices] AppDbContext db, CancellationToken ct)
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId))
            return Unauthorized(new { error = "invalid_token" });

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId, ct);
        if (user is null) return Unauthorized(new { error = "user_not_found" });

        return Ok(new MeResponse(
            user.UserId,
            user.TenantId,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role.ToString(),
            user.Status.ToString()
        ));
    }
}

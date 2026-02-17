using System.Security.Claims;
using CommunityOS.Api.Services;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/me")]
public sealed class MeProfileController : ControllerBase
{
    public sealed record UpdateProfileRequest(string? FirstName, string? LastName, string? City, string? Bio, string? AvatarUrl);

    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult> UpdateProfile([FromBody] UpdateProfileRequest req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var u = await db.Users.FirstOrDefaultAsync(x => x.UserId == me, ct);
        if (u is null) return Unauthorized(new { error = "user_not_found" });

        if (req.FirstName is not null) u.FirstName = req.FirstName.Trim();
        if (req.LastName is not null) u.LastName = req.LastName.Trim();
        u.City = req.City?.Trim();
        u.Bio = req.Bio?.Trim();
        u.AvatarUrl = req.AvatarUrl?.Trim();

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

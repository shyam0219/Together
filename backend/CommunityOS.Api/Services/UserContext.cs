using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CommunityOS.Api.Services;

public static class UserContext
{
    public static Guid GetRequiredUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub")
                  ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrWhiteSpace(sub) || !Guid.TryParse(sub, out var userId) || userId == Guid.Empty)
            throw new InvalidOperationException("Invalid or missing user id claim.");

        return userId;
    }

    public static string? GetRole(ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.Role)
               ?? user.FindFirstValue("role")
               ?? user.FindFirstValue("Role");
    }
}

using CommunityOS.Api.DTOs;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/members")]
public sealed class MembersController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<MemberListItemDto>>> List([FromQuery] string? q, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var query = db.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(u => (u.FirstName + " " + u.LastName).Contains(s) || u.Email.Contains(s));
        }

        var items = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Take(100)
            .ToListAsync(ct);

        return Ok(items.Select(MemberDtoMapper.ToListItem).ToList());
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<MemberListItemDto>> Get([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == id, ct);
        if (u is null) return NotFound(new { error = "not_found" });
        return Ok(MemberDtoMapper.ToListItem(u));
    }
}

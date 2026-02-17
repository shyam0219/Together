using CommunityOS.Api.DTOs;
using CommunityOS.Api.Services;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<PageResponse<NotificationDto>>> List(
        [FromServices] AppDbContext db,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var me = UserContext.GetRequiredUserId(User);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 ? 20 : pageSize;
        pageSize = Math.Min(pageSize, 50);

        // SQLite fallback limitation: DateTimeOffset ordering translation.
        var baseQuery = db.Notifications.AsNoTracking().Where(n => n.UserId == me).Take(2000);
        var all = await baseQuery.ToListAsync(ct);
        var ordered = all.OrderByDescending(n => n.CreatedAt).ToList();

        var skip = (page - 1) * pageSize;
        var items = ordered.Skip(skip).Take(pageSize).ToList();
        var hasMore = ordered.Count > skip + pageSize;

        var dtos = items.Select(n => new NotificationDto(n.NotificationId, n.Type.ToString(), n.PayloadJson, n.IsRead, n.CreatedAt)).ToList();
        return Ok(new PageResponse<NotificationDto>(dtos, page, pageSize, hasMore));
    }

    [HttpPost("{id:guid}/read")]
    [Authorize]
    public async Task<ActionResult> MarkRead([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var n = await db.Notifications.FirstOrDefaultAsync(x => x.NotificationId == id && x.UserId == me, ct);
        if (n is null) return NotFound(new { error = "not_found" });

        n.IsRead = true;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

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
    public async Task<ActionResult<List<NotificationDto>>> List([FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        // SQLite fallback limitation: DateTimeOffset ordering translation.
        // Order on server for SQL Server; for SQLite we order on client.
        var baseQuery = db.Notifications.AsNoTracking().Where(n => n.UserId == me).Take(200);
        var items = await baseQuery.ToListAsync(ct);
        items = items.OrderByDescending(n => n.CreatedAt).Take(100).ToList();

        return Ok(items.Select(n => new NotificationDto(n.NotificationId, n.Type.ToString(), n.PayloadJson, n.IsRead, n.CreatedAt)).ToList());
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

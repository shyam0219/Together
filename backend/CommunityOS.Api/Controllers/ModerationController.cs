using System.Text.Json;
using CommunityOS.Api.DTOs;
using CommunityOS.Api.Services;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/mod")]
public sealed class ModerationController : ControllerBase
{
    private static bool IsModOrAdmin(string? role) =>
        string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, UserRole.Moderator.ToString(), StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, UserRole.PlatformOwner.ToString(), StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, UserRole.TenantOwner.ToString(), StringComparison.OrdinalIgnoreCase);

    [HttpGet("reports")]
    [Authorize]
    public async Task<ActionResult<PageResponse<ReportDto>>> Reports(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromServices] AppDbContext db,
        CancellationToken ct = default)
    {
        var role = UserContext.GetRole(User);
        if (!IsModOrAdmin(role)) return Forbid();

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 ? 50 : pageSize;
        pageSize = Math.Min(pageSize, 100);

        var all = await db.Reports.AsNoTracking().Take(5000).ToListAsync(ct);
        var ordered = all.OrderByDescending(r => r.CreatedAt).ToList();

        var skip = (page - 1) * pageSize;
        var items = ordered.Skip(skip).Take(pageSize).ToList();
        var hasMore = ordered.Count > skip + pageSize;

        var dtos = items.Select(r => new ReportDto(r.ReportId, r.ReporterId, r.TargetType.ToString(), r.TargetId, r.Reason, r.Notes, r.Status.ToString(), r.CreatedAt)).ToList();
        return Ok(new PageResponse<ReportDto>(dtos, page, pageSize, hasMore));
    }

    [HttpPost("reports/{id:guid}/action")]
    [Authorize]
    public async Task<ActionResult> ActionReport(
        [FromRoute] Guid id,
        [FromBody] ModActionRequest req,
        [FromServices] AppDbContext db,
        [FromServices] IAuditService audit,
        CancellationToken ct)
    {
        var actorId = UserContext.GetRequiredUserId(User);
        var role = UserContext.GetRole(User);
        if (!IsModOrAdmin(role)) return Forbid();

        var report = await db.Reports.FirstOrDefaultAsync(r => r.ReportId == id, ct);
        if (report is null) return NotFound(new { error = "not_found" });

        // Allowed actions: Reviewed, Actioned
        if (string.Equals(req.ActionType, "Reviewed", StringComparison.OrdinalIgnoreCase))
            report.Status = ReportStatus.Reviewed;
        else
            report.Status = ReportStatus.Actioned;

        await db.SaveChangesAsync(ct);

        await audit.LogAsync(actorId, "REPORT_ACTION", "Report", report.ReportId, new { req.ActionType, req.Notes, report.TargetType, report.TargetId }, ct);

        // Apply optional content action
        if (string.Equals(req.TargetType, "Post", StringComparison.OrdinalIgnoreCase))
        {
            var post = await db.Posts.FirstOrDefaultAsync(p => p.PostId == req.TargetId, ct);
            if (post is not null)
            {
                if (string.Equals(req.ActionType, "Hide", StringComparison.OrdinalIgnoreCase)) post.Status = PostStatus.Hidden;
                if (string.Equals(req.ActionType, "Remove", StringComparison.OrdinalIgnoreCase)) post.Status = PostStatus.Removed;
                await db.SaveChangesAsync(ct);
                await audit.LogAsync(actorId, "CONTENT_ACTION", "Post", post.PostId, new { req.ActionType, req.Notes }, ct);
            }
        }
        else if (string.Equals(req.TargetType, "Comment", StringComparison.OrdinalIgnoreCase))
        {
            var c = await db.Comments.FirstOrDefaultAsync(x => x.CommentId == req.TargetId, ct);
            if (c is not null)
            {
                c.SoftDeletedAt = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(ct);
                await audit.LogAsync(actorId, "CONTENT_ACTION", "Comment", c.CommentId, new { req.ActionType, req.Notes }, ct);
            }
        }

        return NoContent();
    }

    [HttpPost("users/{id:guid}/suspend")]
    [Authorize]
    public async Task<ActionResult> SuspendUser([FromRoute] Guid id, [FromServices] AppDbContext db, [FromServices] IAuditService audit, CancellationToken ct)
    {
        var actorId = UserContext.GetRequiredUserId(User);
        var role = UserContext.GetRole(User);
        if (!IsModOrAdmin(role)) return Forbid();

        var u = await db.Users.FirstOrDefaultAsync(x => x.UserId == id, ct);
        if (u is null) return NotFound(new { error = "not_found" });

        u.Status = UserStatus.Suspended;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "USER_SUSPEND", "User", u.UserId, new { }, ct);
        return NoContent();
    }

    [HttpPost("users/{id:guid}/ban")]
    [Authorize]
    public async Task<ActionResult> BanUser([FromRoute] Guid id, [FromServices] AppDbContext db, [FromServices] IAuditService audit, CancellationToken ct)
    {
        var actorId = UserContext.GetRequiredUserId(User);
        var role = UserContext.GetRole(User);
        if (!IsModOrAdmin(role)) return Forbid();

        var u = await db.Users.FirstOrDefaultAsync(x => x.UserId == id, ct);
        if (u is null) return NotFound(new { error = "not_found" });

        u.Status = UserStatus.Banned;
        await db.SaveChangesAsync(ct);
        await audit.LogAsync(actorId, "USER_BAN", "User", u.UserId, new { }, ct);
        return NoContent();
    }
}

using CommunityOS.Api.DTOs;
using CommunityOS.Api.Services;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
public sealed class CommentsController : ControllerBase
{
    [HttpGet("api/v1/posts/{postId:guid}/comments")]
    [Authorize]
    public async Task<ActionResult<PageResponse<CommentDto>>> ListForPost(
        [FromRoute] Guid postId,
        [FromServices] AppDbContext db,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 ? 50 : pageSize;
        pageSize = Math.Min(pageSize, 200);

        // Ensure post exists and caller can access it (group privacy is enforced in PostsController GET,
        // but for comments listing we re-check visibility via group mapping).
        var me = UserContext.GetRequiredUserId(User);
        var post = await db.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == postId, ct);
        if (post is null) return NotFound(new { error = "post_not_found" });

        var links = await db.GroupPosts.AsNoTracking()
            .Include(gp => gp.Group)
            .Where(gp => gp.PostId == postId)
            .ToListAsync(ct);
        if (links.Count > 0)
        {
            var myGroupIds = await db.GroupMembers.AsNoTracking()
                .Where(m => m.UserId == me)
                .Select(m => m.GroupId)
                .ToListAsync(ct);
            var myGroupSet = myGroupIds.ToHashSet();

            var visible = links.Any(l => l.Group.Visibility == GroupVisibility.Public || myGroupSet.Contains(l.GroupId));
            if (!visible) return NotFound(new { error = "post_not_found" });
        }

        // Load all comments for the post (SQLite DateTimeOffset ordering limitations -> in-memory sort).
        var raw = await db.Comments.AsNoTracking()
            .Where(c => c.PostId == postId)
            .Take(10000)
            .ToListAsync(ct);

        raw = raw.OrderBy(c => c.CreatedAt).ToList();

        var skip = (page - 1) * pageSize;
        var pageItems = raw.Skip(skip).Take(pageSize).ToList();
        var hasMore = raw.Count > skip + pageSize;

        var authorIds = pageItems.Select(c => c.AuthorId).Distinct().ToList();
        var authors = await db.Users.AsNoTracking()
            .Where(u => authorIds.Contains(u.UserId))
            .Select(u => new { u.UserId, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(ct);
        var authorMap = authors.ToDictionary(a => a.UserId, a => a.Name);

        var dtos = pageItems.Select(c => new CommentDto(
            c.CommentId,
            c.PostId,
            c.AuthorId,
            authorMap.TryGetValue(c.AuthorId, out var name) ? name : "Unknown",
            c.ParentCommentId,
            c.Text,
            c.CreatedAt
        )).ToList();

        return Ok(new PageResponse<CommentDto>(dtos, page, pageSize, hasMore));
    }

    private static readonly TimeSpan CommentRateWindow = TimeSpan.FromSeconds(10);

    [HttpPost("api/v1/posts/{postId:guid}/comments")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> Create(
        [FromRoute] Guid postId,
        [FromBody] CreateCommentRequest req,
        [FromServices] AppDbContext db,
        [FromServices] IRateLimitService rateLimiter,
        [FromServices] ITenantProvider tenantProvider,
        [FromServices] INotificationService notifications,
        CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        if (!rateLimiter.TryConsume(tenantProvider.CurrentTenantId, me, "create_comment", CommentRateWindow, out var retryAfter))
            return StatusCode(429, new { error = "rate_limited", retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds) });

        var post = await db.Posts.FirstOrDefaultAsync(p => p.PostId == postId, ct);
        if (post is null) return NotFound(new { error = "post_not_found" });
        if (!post.CommentingEnabled) return BadRequest(new { error = "commenting_disabled" });

        if (string.IsNullOrWhiteSpace(req.Text)) return BadRequest(new { error = "missing_text" });

        if (req.ParentCommentId.HasValue)
        {
            var parent = await db.Comments.AsNoTracking().FirstOrDefaultAsync(c => c.CommentId == req.ParentCommentId.Value && c.PostId == postId, ct);
            if (parent is null) return BadRequest(new { error = "invalid_parent_comment" });
        }

        var cmt = new Comment
        {
            CommentId = Guid.NewGuid(),
            PostId = postId,
            AuthorId = me,
            ParentCommentId = req.ParentCommentId,
            Text = req.Text.Trim()
        };

        db.Comments.Add(cmt);
        await db.SaveChangesAsync(ct);

        // Mentions notifications
        var usernames = MentionParser.ExtractMentions(cmt.Text);
        if (usernames.Count > 0)
        {
            var candidates = await db.Users.AsNoTracking()
                .Select(u => new { u.UserId, u.Email })
                .ToListAsync(ct);

            var mentioned = candidates
                .Where(x => usernames.Contains(x.Email.Split('@')[0], StringComparer.OrdinalIgnoreCase))
                .Select(x => x.UserId)
                .ToList();

            foreach (var uid in mentioned.Distinct())
            {
                if (uid == me) continue;
                await notifications.CreateMentionAsync(uid, me, "Comment", cmt.CommentId, ct);
            }
        }

        var author = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == me, ct);
        return Ok(new CommentDto(
            cmt.CommentId,
            cmt.PostId,
            cmt.AuthorId,
            author is null ? "Unknown" : (author.FirstName + " " + author.LastName),
            cmt.ParentCommentId,
            cmt.Text,
            cmt.CreatedAt
        ));
    }

    [HttpPut("api/v1/comments/{id:guid}")]
    [Authorize]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] UpdateCommentRequest req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var c = await db.Comments.FirstOrDefaultAsync(x => x.CommentId == id, ct);
        if (c is null) return NotFound(new { error = "not_found" });

        if (c.AuthorId != me) return Forbid();

        c.Text = req.Text.Trim();
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("api/v1/comments/{id:guid}")]
    [Authorize]
    public async Task<ActionResult> Delete([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var c = await db.Comments.FirstOrDefaultAsync(x => x.CommentId == id, ct);
        if (c is null) return NotFound(new { error = "not_found" });

        if (c.AuthorId != me) return Forbid();

        c.SoftDeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

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
    private static readonly TimeSpan CommentRateWindow = TimeSpan.FromSeconds(10);

    [HttpPost("api/v1/posts/{postId:guid}/comments")]
    [Authorize]
    public async Task<ActionResult<CommentDto>> Create(
        [FromRoute] Guid postId,
        [FromBody] CreateCommentRequest req,
        [FromServices] AppDbContext db,
        [FromServices] IRateLimitService rateLimiter,
        [FromServices] INotificationService notifications,
        CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        if (!rateLimiter.TryConsume(me, "create_comment", CommentRateWindow, out var retryAfter))
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
                .Where(u => usernames.Contains(u.Email.Split('@')[0]))
                .Select(u => u.UserId)
                .ToListAsync(ct);

            foreach (var uid in candidates.Distinct())
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

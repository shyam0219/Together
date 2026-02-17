using CommunityOS.Api.DTOs;
using CommunityOS.Api.Services;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/posts")]
public sealed class PostsController : ControllerBase
{
    private static readonly TimeSpan PostRateWindow = TimeSpan.FromSeconds(30);

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<PostDto>>> List([
        FromServices] AppDbContext db,
        CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        var posts = await db.Posts
            .AsNoTracking()
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        var postIds = posts.Select(p => p.PostId).ToList();

        var likeCounts = await db.Reactions
            .AsNoTracking()
            .Where(r => postIds.Contains(r.PostId) && r.Type == ReactionType.Like)
            .GroupBy(r => r.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var myLikes = await db.Reactions.AsNoTracking()
            .Where(r => postIds.Contains(r.PostId) && r.UserId == me && r.Type == ReactionType.Like)
            .Select(r => r.PostId)
            .ToListAsync(ct);

        var myBookmarks = await db.Bookmarks.AsNoTracking()
            .Where(b => postIds.Contains(b.PostId) && b.UserId == me)
            .Select(b => b.PostId)
            .ToListAsync(ct);

        // Fetch authors in one query
        var authorIds = posts.Select(p => p.AuthorId).Distinct().ToList();
        var authors = await db.Users.AsNoTracking()
            .Where(u => authorIds.Contains(u.UserId))
            .Select(u => new { u.UserId, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(ct);
        var authorMap = authors.ToDictionary(a => a.UserId, a => a.Name);

        var likeCountMap = likeCounts.ToDictionary(x => x.PostId, x => x.Count);
        var myLikeSet = myLikes.ToHashSet();
        var myBookmarkSet = myBookmarks.ToHashSet();

        var dtos = posts.Select(p => new PostDto(
            p.PostId,
            p.AuthorId,
            authorMap.TryGetValue(p.AuthorId, out var name) ? name : "Unknown",
            p.BodyText,
            p.LinkUrl,
            p.LinkTitle,
            p.LinkDescription,
            p.LinkImageUrl,
            p.CommentingEnabled,
            p.Status.ToString(),
            p.CreatedAt,
            p.Images.OrderBy(i => i.SortOrder).Select(PostDtoMapper.ToDto).ToList(),
            likeCountMap.TryGetValue(p.PostId, out var cnt) ? cnt : 0,
            myLikeSet.Contains(p.PostId),
            myBookmarkSet.Contains(p.PostId)
        )).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PostDto>> Create(
        [FromBody] CreatePostRequest req,
        [FromServices] AppDbContext db,
        [FromServices] IRateLimitService rateLimiter,
        [FromServices] INotificationService notifications,
        CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        if (!rateLimiter.TryConsume(me, "create_post", PostRateWindow, out var retryAfter))
            return StatusCode(429, new { error = "rate_limited", retryAfterSeconds = (int)Math.Ceiling(retryAfter.TotalSeconds) });

        if (string.IsNullOrWhiteSpace(req.BodyText))
            return BadRequest(new { error = "missing_body" });

        var urls = req.ImageUrls ?? new();
        if (urls.Count is < 0 or > 10)
            return BadRequest(new { error = "invalid_image_count" });

        var post = new Post
        {
            PostId = Guid.NewGuid(),
            AuthorId = me,
            BodyText = req.BodyText.Trim(),
            LinkUrl = req.LinkUrl,
            LinkTitle = req.LinkTitle,
            LinkDescription = req.LinkDescription,
            LinkImageUrl = req.LinkImageUrl,
            CommentingEnabled = true,
            Status = PostStatus.Active
        };

        post.Images = urls.Select((u, idx) => new PostImage
        {
            PostImageId = Guid.NewGuid(),
            PostId = post.PostId,
            Url = u,
            SortOrder = idx
        }).ToList();

        db.Posts.Add(post);
        await db.SaveChangesAsync(ct);

        // Mentions -> notifications (within tenant due to filters)
        var usernames = MentionParser.ExtractMentions(post.BodyText);
        if (usernames.Count > 0)
        {
            // Simplified mention resolution: match by email local-part
            var candidates = await db.Users.AsNoTracking()
                .Select(u => new { u.UserId, u.Email })
                .ToListAsync(ct);

            var mentioned = candidates
                .Where(x => usernames.Contains(x.Email.Split('@')[0], StringComparer.OrdinalIgnoreCase))
                .Select(x => x.UserId)
                .ToList();

            foreach (var uid in candidates.Distinct())
            {
                if (uid == me) continue;
                await notifications.CreateMentionAsync(uid, me, "Post", post.PostId, ct);
            }
        }

        // Return single post via GET logic
        return await Get(post.PostId, db, ct);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<PostDto>> Get([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        var p = await db.Posts.AsNoTracking()
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.PostId == id, ct);

        if (p is null) return NotFound(new { error = "not_found" });

        var author = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == p.AuthorId, ct);
        var likeCount = await db.Reactions.AsNoTracking().CountAsync(r => r.PostId == p.PostId && r.Type == ReactionType.Like, ct);
        var likedByMe = await db.Reactions.AsNoTracking().AnyAsync(r => r.PostId == p.PostId && r.UserId == me && r.Type == ReactionType.Like, ct);
        var bookmarkedByMe = await db.Bookmarks.AsNoTracking().AnyAsync(b => b.PostId == p.PostId && b.UserId == me, ct);

        return Ok(new PostDto(
            p.PostId,
            p.AuthorId,
            author is null ? "Unknown" : (author.FirstName + " " + author.LastName),
            p.BodyText,
            p.LinkUrl,
            p.LinkTitle,
            p.LinkDescription,
            p.LinkImageUrl,
            p.CommentingEnabled,
            p.Status.ToString(),
            p.CreatedAt,
            p.Images.OrderBy(i => i.SortOrder).Select(PostDtoMapper.ToDto).ToList(),
            likeCount,
            likedByMe,
            bookmarkedByMe
        ));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> Update([FromRoute] Guid id, [FromBody] UpdatePostRequest req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var p = await db.Posts.FirstOrDefaultAsync(x => x.PostId == id, ct);
        if (p is null) return NotFound(new { error = "not_found" });

        if (p.AuthorId != me) return Forbid();

        p.BodyText = req.BodyText.Trim();
        if (req.CommentingEnabled.HasValue) p.CommentingEnabled = req.CommentingEnabled.Value;

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<ActionResult> Delete([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var p = await db.Posts.FirstOrDefaultAsync(x => x.PostId == id, ct);
        if (p is null) return NotFound(new { error = "not_found" });

        if (p.AuthorId != me) return Forbid();

        p.SoftDeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/images")]
    [Authorize]
    public async Task<ActionResult> AddImages([FromRoute] Guid id, [FromBody] AddPostImagesRequest req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var p = await db.Posts.Include(x => x.Images).FirstOrDefaultAsync(x => x.PostId == id, ct);
        if (p is null) return NotFound(new { error = "not_found" });

        if (p.AuthorId != me) return Forbid();

        if (req.ImageUrls is null || req.ImageUrls.Count == 0) return BadRequest(new { error = "missing_images" });
        if (p.Images.Count + req.ImageUrls.Count > 10) return BadRequest(new { error = "too_many_images" });

        var start = p.Images.Count;
        foreach (var (url, idx) in req.ImageUrls.Select((u, i) => (u, i)))
        {
            p.Images.Add(new PostImage
            {
                PostImageId = Guid.NewGuid(),
                PostId = p.PostId,
                Url = url,
                SortOrder = start + idx
            });
        }

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/like")]
    [Authorize]
    public async Task<ActionResult> Like([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var exists = await db.Posts.AsNoTracking().AnyAsync(p => p.PostId == id, ct);
        if (!exists) return NotFound(new { error = "not_found" });

        var already = await db.Reactions.AnyAsync(r => r.PostId == id && r.UserId == me && r.Type == ReactionType.Like, ct);
        if (already) return NoContent();

        db.Reactions.Add(new Reaction { ReactionId = Guid.NewGuid(), PostId = id, UserId = me, Type = ReactionType.Like });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/like")]
    [Authorize]
    public async Task<ActionResult> Unlike([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var r = await db.Reactions.FirstOrDefaultAsync(x => x.PostId == id && x.UserId == me && x.Type == ReactionType.Like, ct);
        if (r is null) return NoContent();

        db.Reactions.Remove(r);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/bookmark")]
    [Authorize]
    public async Task<ActionResult> Bookmark([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var exists = await db.Posts.AsNoTracking().AnyAsync(p => p.PostId == id, ct);
        if (!exists) return NotFound(new { error = "not_found" });

        var already = await db.Bookmarks.AnyAsync(b => b.PostId == id && b.UserId == me, ct);
        if (already) return NoContent();

        db.Bookmarks.Add(new Bookmark { BookmarkId = Guid.NewGuid(), PostId = id, UserId = me });
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/bookmark")]
    [Authorize]
    public async Task<ActionResult> Unbookmark([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var b = await db.Bookmarks.FirstOrDefaultAsync(x => x.PostId == id && x.UserId == me, ct);
        if (b is null) return NoContent();

        db.Bookmarks.Remove(b);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

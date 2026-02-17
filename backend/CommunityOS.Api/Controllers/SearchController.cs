using CommunityOS.Api.DTOs;
using CommunityOS.Api.Services;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/search")]
public sealed class SearchController : ControllerBase
{
    [HttpGet("posts")]
    [Authorize]
    public async Task<ActionResult<PageResponse<PostDto>>> SearchPosts(
        [FromQuery] string q,
        [FromServices] AppDbContext db,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "missing_q" });

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 ? 20 : pageSize;
        pageSize = Math.Min(pageSize, 50);

        var me = UserContext.GetRequiredUserId(User);
        var query = db.Posts.AsNoTracking().Include(p => p.Images);

        var s = q.Trim();
        query = query.Where(p =>
            p.BodyText.Contains(s) ||
            (p.LinkTitle != null && p.LinkTitle.Contains(s)) ||
            (p.LinkDescription != null && p.LinkDescription.Contains(s))
        );

        // We must also enforce group privacy; reuse the same approach as /posts: load mapping and filter in-memory.
        var myGroupIds = await db.GroupMembers.AsNoTracking()
            .Where(m => m.UserId == me)
            .Select(m => m.GroupId)
            .ToListAsync(ct);
        var myGroupSet = myGroupIds.ToHashSet();

        var groupPosts = await db.GroupPosts.AsNoTracking()
            .Include(gp => gp.Group)
            .Take(20000)
            .ToListAsync(ct);

        bool IsVisible(Post p)
        {
            var links = groupPosts.Where(gp => gp.PostId == p.PostId).ToList();
            if (links.Count == 0) return true;
            return links.Any(l => l.Group.Visibility == GroupVisibility.Public || myGroupSet.Contains(l.GroupId));
        }

        var raw = await query.Take(5000).ToListAsync(ct);
        raw = raw.Where(IsVisible).OrderByDescending(p => p.CreatedAt).ToList();

        var skip = (page - 1) * pageSize;
        var pageItems = raw.Skip(skip).Take(pageSize).ToList();
        var hasMore = raw.Count > skip + pageSize;

        var postIds = pageItems.Select(p => p.PostId).ToList();

        var likeCounts = await db.Reactions.AsNoTracking()
            .Where(r => postIds.Contains(r.PostId) && r.Type == ReactionType.Like)
            .GroupBy(r => r.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var likeCountMap = likeCounts.ToDictionary(x => x.PostId, x => x.Count);

        var commentCounts = await db.Comments.AsNoTracking()
            .Where(c => postIds.Contains(c.PostId))
            .GroupBy(c => c.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var commentCountMap = commentCounts.ToDictionary(x => x.PostId, x => x.Count);

        var myLikes = await db.Reactions.AsNoTracking()
            .Where(r => postIds.Contains(r.PostId) && r.UserId == me && r.Type == ReactionType.Like)
            .Select(r => r.PostId)
            .ToListAsync(ct);
        var myLikeSet = myLikes.ToHashSet();

        var myBookmarks = await db.Bookmarks.AsNoTracking()
            .Where(b => postIds.Contains(b.PostId) && b.UserId == me)
            .Select(b => b.PostId)
            .ToListAsync(ct);
        var myBookmarkSet = myBookmarks.ToHashSet();

        var authorIds = pageItems.Select(p => p.AuthorId).Distinct().ToList();
        var authors = await db.Users.AsNoTracking()
            .Where(u => authorIds.Contains(u.UserId))
            .Select(u => new { u.UserId, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(ct);
        var authorMap = authors.ToDictionary(a => a.UserId, a => a.Name);

        var dtos = pageItems.Select(p => new PostDto(
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
            likeCountMap.TryGetValue(p.PostId, out var lc) ? lc : 0,
            commentCountMap.TryGetValue(p.PostId, out var cc) ? cc : 0,
            myLikeSet.Contains(p.PostId),
            myBookmarkSet.Contains(p.PostId)
        )).ToList();

        return Ok(new PageResponse<PostDto>(dtos, page, pageSize, hasMore));
    }

    [HttpGet("members")]
    [Authorize]
    public async Task<ActionResult<PageResponse<MemberListItemDto>>> SearchMembers(
        [FromQuery] string q,
        [FromServices] AppDbContext db,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "missing_q" });

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 ? 20 : pageSize;
        pageSize = Math.Min(pageSize, 50);

        var s = q.Trim();
        var raw = await db.Users.AsNoTracking()
            .Where(u => (u.FirstName + " " + u.LastName).Contains(s) || u.Email.Contains(s))
            .Take(2000)
            .ToListAsync(ct);

        raw = raw.OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToList();
        var skip = (page - 1) * pageSize;
        var pageItems = raw.Skip(skip).Take(pageSize).ToList();
        var hasMore = raw.Count > skip + pageSize;

        var dtos = pageItems.Select(MemberDtoMapper.ToListItem).ToList();
        return Ok(new PageResponse<MemberListItemDto>(dtos, page, pageSize, hasMore));
    }

    [HttpGet("groups")]
    [Authorize]
    public async Task<ActionResult<PageResponse<GroupDto>>> SearchGroups(
        [FromQuery] string q,
        [FromServices] AppDbContext db,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { error = "missing_q" });

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 ? 20 : pageSize;
        pageSize = Math.Min(pageSize, 50);

        var me = UserContext.GetRequiredUserId(User);
        var s = q.Trim();

        var raw = await db.Groups.AsNoTracking()
            .Where(g => g.Name.Contains(s) || (g.Description != null && g.Description.Contains(s)))
            .Take(2000)
            .ToListAsync(ct);

        raw = raw.OrderBy(g => g.Name).ToList();
        var skip = (page - 1) * pageSize;
        var pageItems = raw.Skip(skip).Take(pageSize).ToList();
        var hasMore = raw.Count > skip + pageSize;

        var groupIds = pageItems.Select(g => g.GroupId).ToList();
        var counts = await db.GroupMembers.AsNoTracking()
            .Where(m => groupIds.Contains(m.GroupId))
            .GroupBy(m => m.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        var countMap = counts.ToDictionary(x => x.GroupId, x => x.Count);

        var myMemberships = await db.GroupMembers.AsNoTracking()
            .Where(m => groupIds.Contains(m.GroupId) && m.UserId == me)
            .Select(m => m.GroupId)
            .ToListAsync(ct);
        var memberSet = myMemberships.ToHashSet();

        var dtos = pageItems.Select(g => new GroupDto(
            g.GroupId,
            g.Name,
            g.Description,
            g.Visibility.ToString(),
            g.CreatedById,
            g.CreatedAt,
            countMap.TryGetValue(g.GroupId, out var c) ? c : 0,
            memberSet.Contains(g.GroupId)
        )).ToList();

        return Ok(new PageResponse<GroupDto>(dtos, page, pageSize, hasMore));
    }
}

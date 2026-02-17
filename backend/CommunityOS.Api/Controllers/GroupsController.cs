using CommunityOS.Api.DTOs;
using CommunityOS.Api.Services;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/groups")]
public sealed class GroupsController : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<GroupDto>>> List([FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        var groups = await db.Groups.AsNoTracking()
            .OrderBy(g => g.Name)
            .Take(100)
            .ToListAsync(ct);

        var groupIds = groups.Select(g => g.GroupId).ToList();

        var counts = await db.GroupMembers.AsNoTracking()
            .Where(m => groupIds.Contains(m.GroupId))
            .GroupBy(m => m.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var myMemberships = await db.GroupMembers.AsNoTracking()
            .Where(m => groupIds.Contains(m.GroupId) && m.UserId == me)
            .Select(m => m.GroupId)
            .ToListAsync(ct);

        var countMap = counts.ToDictionary(x => x.GroupId, x => x.Count);
        var memberSet = myMemberships.ToHashSet();

        var dtos = groups.Select(g => new GroupDto(
            g.GroupId,
            g.Name,
            g.Description,
            g.Visibility.ToString(),
            g.CreatedById,
            g.CreatedAt,
            countMap.TryGetValue(g.GroupId, out var c) ? c : 0,
            memberSet.Contains(g.GroupId)
        )).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<GroupDto>> Create([FromBody] CreateGroupRequest req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest(new { error = "missing_name" });

        var visibility = string.Equals(req.Visibility, "Private", StringComparison.OrdinalIgnoreCase)
            ? GroupVisibility.Private
            : GroupVisibility.Public;

        var g = new Group
        {
            GroupId = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Description = req.Description?.Trim(),
            Visibility = visibility,
            CreatedById = me
        };

        db.Groups.Add(g);
        // creator auto-joins
        db.GroupMembers.Add(new GroupMember
        {
            GroupMemberId = Guid.NewGuid(),
            GroupId = g.GroupId,
            UserId = me,
            Role = GroupMemberRole.Moderator,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return Ok(new GroupDto(g.GroupId, g.Name, g.Description, g.Visibility.ToString(), g.CreatedById, g.CreatedAt, 1, true));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<GroupDto>> Get([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var g = await db.Groups.AsNoTracking().FirstOrDefaultAsync(x => x.GroupId == id, ct);
        if (g is null) return NotFound(new { error = "not_found" });

        var memberCount = await db.GroupMembers.AsNoTracking().CountAsync(m => m.GroupId == id, ct);
        var isMember = await db.GroupMembers.AsNoTracking().AnyAsync(m => m.GroupId == id && m.UserId == me, ct);

        return Ok(new GroupDto(g.GroupId, g.Name, g.Description, g.Visibility.ToString(), g.CreatedById, g.CreatedAt, memberCount, isMember));
    }

    [HttpPost("{id:guid}/join")]
    [Authorize]
    public async Task<ActionResult> Join([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var g = await db.Groups.FirstOrDefaultAsync(x => x.GroupId == id, ct);
        if (g is null) return NotFound(new { error = "not_found" });

        if (g.Visibility == GroupVisibility.Private)
            return Forbid();

        var exists = await db.GroupMembers.AnyAsync(m => m.GroupId == id && m.UserId == me, ct);
        if (exists) return NoContent();

        db.GroupMembers.Add(new GroupMember
        {
            GroupMemberId = Guid.NewGuid(),
            GroupId = id,
            UserId = me,
            Role = GroupMemberRole.Member,
            JoinedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/leave")]
    [Authorize]
    public async Task<ActionResult> Leave([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var membership = await db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == id && m.UserId == me, ct);
        if (membership is null) return NoContent();

        db.GroupMembers.Remove(membership);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

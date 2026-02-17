using CommunityOS.Api.DTOs;
using CommunityOS.Api.Services;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class PollsController : ControllerBase
{
    [HttpPost("posts/{postId:guid}/poll")]
    [Authorize]
    public async Task<ActionResult<PollDto>> Create(
        [FromRoute] Guid postId,
        [FromBody] CreatePollRequest req,
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var post = await db.Posts.FirstOrDefaultAsync(p => p.PostId == postId, ct);
        if (post is null) return NotFound(new { error = "post_not_found" });

        if (post.AuthorId != me) return Forbid();

        var existing = await db.Polls.AnyAsync(p => p.PostId == postId, ct);
        if (existing) return Conflict(new { error = "poll_already_exists" });

        if (string.IsNullOrWhiteSpace(req.Question) || req.Options is null || req.Options.Count < 2 || req.Options.Count > 10)
            return BadRequest(new { error = "invalid_poll" });

        var poll = new Poll
        {
            PollId = Guid.NewGuid(),
            PostId = postId,
            Question = req.Question.Trim(),
        };

        db.Polls.Add(poll);
        await db.SaveChangesAsync(ct);

        var opts = req.Options.Select((t, idx) => new PollOption
        {
            PollOptionId = Guid.NewGuid(),
            PollId = poll.PollId,
            Text = t.Trim(),
            SortOrder = idx
        }).ToList();

        db.PollOptions.AddRange(opts);
        await db.SaveChangesAsync(ct);

        return Ok(new PollDto(poll.PollId, poll.PostId, poll.Question, opts.Select(o => new PollOptionDto(o.PollOptionId, o.Text, o.SortOrder, 0)).ToList(), 0, null));
    }

    [HttpGet("posts/{postId:guid}/poll")]
    [Authorize]
    public async Task<ActionResult<PollDto>> GetForPost([FromRoute] Guid postId, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        var poll = await db.Polls.AsNoTracking().FirstOrDefaultAsync(p => p.PostId == postId, ct);
        if (poll is null) return NotFound(new { error = "not_found" });

        var options = await db.PollOptions.AsNoTracking().Where(o => o.PollId == poll.PollId).ToListAsync(ct);
        options = options.OrderBy(o => o.SortOrder).ToList();

        var votes = await db.PollVotes.AsNoTracking().Where(v => v.PollId == poll.PollId).ToListAsync(ct);
        var counts = votes.GroupBy(v => v.PollOptionId).ToDictionary(g => g.Key, g => g.Count());
        var myVote = votes.FirstOrDefault(v => v.UserId == me);

        var optDtos = options.Select(o => new PollOptionDto(o.PollOptionId, o.Text, o.SortOrder, counts.TryGetValue(o.PollOptionId, out var c) ? c : 0)).ToList();
        var total = votes.Count;

        return Ok(new PollDto(poll.PollId, poll.PostId, poll.Question, optDtos, total, myVote?.PollOptionId));
    }

    [HttpPost("polls/{pollId:guid}/vote")]
    [Authorize]
    public async Task<ActionResult> Vote([FromRoute] Guid pollId, [FromBody] VoteRequest req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        var poll = await db.Polls.AsNoTracking().FirstOrDefaultAsync(p => p.PollId == pollId, ct);
        if (poll is null) return NotFound(new { error = "not_found" });

        var option = await db.PollOptions.AsNoTracking().FirstOrDefaultAsync(o => o.PollOptionId == req.OptionId && o.PollId == pollId, ct);
        if (option is null) return BadRequest(new { error = "invalid_option" });

        var already = await db.PollVotes.AnyAsync(v => v.PollId == pollId && v.UserId == me, ct);
        if (already) return Conflict(new { error = "already_voted" });

        db.PollVotes.Add(new PollVote
        {
            PollVoteId = Guid.NewGuid(),
            PollId = pollId,
            PollOptionId = req.OptionId,
            UserId = me
        });

        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

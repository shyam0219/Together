using CommunityOS.Api.DTOs;
using CommunityOS.Api.Services;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/messages")]
public sealed class MessagesController : ControllerBase
{
    private static Guid CanonA(Guid a, Guid b) => a.CompareTo(b) <= 0 ? a : b;
    private static Guid CanonB(Guid a, Guid b) => a.CompareTo(b) <= 0 ? b : a;

    [HttpGet("conversations")]
    [Authorize]
    public async Task<ActionResult<PageResponse<ConversationListItemDto>>> ListConversations(
        [FromServices] AppDbContext db,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var me = UserContext.GetRequiredUserId(User);

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 ? 20 : pageSize;
        pageSize = Math.Min(pageSize, 50);

        // Get all conversationIds for me
        var myParts = await db.ConversationParticipants.AsNoTracking()
            .Where(p => p.UserId == me)
            .Take(5000)
            .ToListAsync(ct);

        var convoIds = myParts.Select(p => p.ConversationId).Distinct().ToList();
        if (convoIds.Count == 0)
            return Ok(new PageResponse<ConversationListItemDto>(new List<ConversationListItemDto>(), page, pageSize, false));

        // Load conversations and their participants
        var convos = await db.Conversations.AsNoTracking()
            .Where(c => convoIds.Contains(c.ConversationId))
            .Take(5000)
            .ToListAsync(ct);

        // Load participants for these convos
        var parts = await db.ConversationParticipants.AsNoTracking()
            .Where(p => convoIds.Contains(p.ConversationId))
            .Take(20000)
            .ToListAsync(ct);

        // Load last messages (in-memory ordering for SQLite)
        var messages = await db.Messages.AsNoTracking()
            .Where(m => convoIds.Contains(m.ConversationId))
            .Take(50000)
            .ToListAsync(ct);

        var lastByConvo = messages
            .GroupBy(m => m.ConversationId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(x => x.SentAt).FirstOrDefault()
            );

        var partMap = parts.GroupBy(p => p.ConversationId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Compute unread: messages after my LastReadAt
        var myLastReadMap = myParts.ToDictionary(p => p.ConversationId, p => p.LastReadAt);
        var unreadMap = new Dictionary<Guid, int>();
        foreach (var cid in convoIds)
        {
            var lastRead = myLastReadMap.TryGetValue(cid, out var lr) ? lr : null;
            var unread = messages.Count(m => m.ConversationId == cid && m.SentAt > (lastRead ?? DateTimeOffset.MinValue) && m.SenderId != me);
            unreadMap[cid] = unread;
        }

        var ordered = convos
            .Select(c => new
            {
                c.ConversationId,
                Last = lastByConvo.TryGetValue(c.ConversationId, out var lm) ? lm : null
            })
            .OrderByDescending(x => x.Last?.SentAt ?? DateTimeOffset.MinValue)
            .Select(x => x.ConversationId)
            .ToList();

        var skip = (page - 1) * pageSize;
        var pageIds = ordered.Skip(skip).Take(pageSize).ToList();
        var hasMore = ordered.Count > skip + pageSize;

        var dtos = pageIds.Select(cid =>
        {
            var pids = partMap.TryGetValue(cid, out var plist) ? plist.Select(p => p.UserId).ToList() : new List<Guid>();
            var last = lastByConvo.TryGetValue(cid, out var lm) ? lm : null;
            return new ConversationListItemDto(
                cid,
                pids,
                last?.BodyText is null ? null : (last.BodyText.Length > 80 ? last.BodyText[..80] : last.BodyText),
                last?.SentAt,
                unreadMap.TryGetValue(cid, out var u) ? u : 0
            );
        }).ToList();

        return Ok(new PageResponse<ConversationListItemDto>(dtos, page, pageSize, hasMore));
    }

    [HttpPost("conversations")]
    [Authorize]
    public async Task<ActionResult<ConversationListItemDto>> StartConversation(
        [FromBody] StartConversationRequest req,
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        if (req.OtherUserId == Guid.Empty || req.OtherUserId == me) return BadRequest(new { error = "invalid_other_user" });

        // Ensure other user exists in same tenant (tenant filters ensure this)
        var other = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == req.OtherUserId, ct);
        if (other is null) return NotFound(new { error = "user_not_found" });

        var a = CanonA(me, req.OtherUserId);
        var b = CanonB(me, req.OtherUserId);

        // Find existing direct conversation
        var existing = await db.Conversations.FirstOrDefaultAsync(c => c.DirectUserAId == a && c.DirectUserBId == b, ct);
        if (existing is not null)
        {
            return Ok(new ConversationListItemDto(existing.ConversationId, new List<Guid> { me, req.OtherUserId }, null, null, 0));
        }

        var convo = new Conversation
        {
            ConversationId = Guid.NewGuid(),
            DirectUserAId = a,
            DirectUserBId = b,
        };

        db.Conversations.Add(convo);
        db.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            ConversationId = convo.ConversationId,
            UserId = me,
            JoinedAt = DateTimeOffset.UtcNow,
            LastReadAt = DateTimeOffset.UtcNow
        });
        db.ConversationParticipants.Add(new ConversationParticipant
        {
            ConversationParticipantId = Guid.NewGuid(),
            ConversationId = convo.ConversationId,
            UserId = req.OtherUserId,
            JoinedAt = DateTimeOffset.UtcNow,
            LastReadAt = null
        });

        await db.SaveChangesAsync(ct);

        return Ok(new ConversationListItemDto(convo.ConversationId, new List<Guid> { me, req.OtherUserId }, null, null, 0));
    }

    [HttpGet("conversations/{id:guid}")]
    [Authorize]
    public async Task<ActionResult> GetConversation([FromRoute] Guid id, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var isPart = await db.ConversationParticipants.AsNoTracking().AnyAsync(p => p.ConversationId == id && p.UserId == me, ct);
        if (!isPart) return NotFound(new { error = "not_found" });

        return Ok(new { conversationId = id });
    }

    [HttpGet("conversations/{id:guid}/messages")]
    [Authorize]
    public async Task<ActionResult<PageResponse<MessageDto>>> ListMessages(
        [FromRoute] Guid id,
        [FromServices] AppDbContext db,
        CancellationToken ct,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var me = UserContext.GetRequiredUserId(User);
        var isPart = await db.ConversationParticipants.AsNoTracking().AnyAsync(p => p.ConversationId == id && p.UserId == me, ct);
        if (!isPart) return NotFound(new { error = "not_found" });

        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 ? 50 : pageSize;
        pageSize = Math.Min(pageSize, 200);

        var raw = await db.Messages.AsNoTracking().Where(m => m.ConversationId == id).Take(20000).ToListAsync(ct);
        raw = raw.OrderByDescending(m => m.SentAt).ToList();

        var skip = (page - 1) * pageSize;
        var pageItems = raw.Skip(skip).Take(pageSize).ToList();
        var hasMore = raw.Count > skip + pageSize;

        // sender names
        var senderIds = pageItems.Select(m => m.SenderId).Distinct().ToList();
        var senders = await db.Users.AsNoTracking()
            .Where(u => senderIds.Contains(u.UserId))
            .Select(u => new { u.UserId, Name = u.FirstName + " " + u.LastName })
            .ToListAsync(ct);
        var senderMap = senders.ToDictionary(s => s.UserId, s => s.Name);

        // return ascending order for chat UI
        pageItems = pageItems.OrderBy(m => m.SentAt).ToList();

        var dtos = pageItems.Select(m => new MessageDto(
            m.MessageId,
            m.ConversationId,
            m.SenderId,
            senderMap.TryGetValue(m.SenderId, out var n) ? n : "Unknown",
            m.BodyText,
            m.SentAt
        )).ToList();

        return Ok(new PageResponse<MessageDto>(dtos, page, pageSize, hasMore));
    }

    [HttpPost("conversations/{id:guid}/messages")]
    [Authorize]
    public async Task<ActionResult<MessageDto>> Send(
        [FromRoute] Guid id,
        [FromBody] SendMessageRequest req,
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var isPart = await db.ConversationParticipants.AsNoTracking().AnyAsync(p => p.ConversationId == id && p.UserId == me, ct);
        if (!isPart) return NotFound(new { error = "not_found" });

        if (string.IsNullOrWhiteSpace(req.BodyText)) return BadRequest(new { error = "missing_body" });

        var msg = new Message
        {
            MessageId = Guid.NewGuid(),
            ConversationId = id,
            SenderId = me,
            BodyText = req.BodyText.Trim(),
            SentAt = DateTimeOffset.UtcNow
        };

        db.Messages.Add(msg);
        await db.SaveChangesAsync(ct);

        var sender = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == me, ct);
        return Ok(new MessageDto(msg.MessageId, msg.ConversationId, msg.SenderId, sender is null ? "Unknown" : (sender.FirstName + " " + sender.LastName), msg.BodyText, msg.SentAt));
    }

    [HttpPost("conversations/{id:guid}/read")]
    [Authorize]
    public async Task<ActionResult> MarkRead(
        [FromRoute] Guid id,
        [FromBody] MarkReadRequest req,
        [FromServices] AppDbContext db,
        CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);
        var p = await db.ConversationParticipants.FirstOrDefaultAsync(x => x.ConversationId == id && x.UserId == me, ct);
        if (p is null) return NotFound(new { error = "not_found" });

        p.LastReadAt = req.ReadAt ?? DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}

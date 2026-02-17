using CommunityOS.Api.DTOs;
using CommunityOS.Api.Services;
using CommunityOS.Domain.Entities;
using CommunityOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityOS.Api.Controllers;

[ApiController]
[Route("api/v1/reports")]
public sealed class ReportsController : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReportDto>> Create([FromBody] CreateReportRequest req, [FromServices] AppDbContext db, CancellationToken ct)
    {
        var me = UserContext.GetRequiredUserId(User);

        if (string.IsNullOrWhiteSpace(req.TargetType) || req.TargetId == Guid.Empty || string.IsNullOrWhiteSpace(req.Reason))
            return BadRequest(new { error = "missing_fields" });

        var targetType = req.TargetType.Trim();
        if (!Enum.TryParse<ReportTargetType>(targetType, true, out var parsed))
            return BadRequest(new { error = "invalid_target_type" });

        var report = new Report
        {
            ReportId = Guid.NewGuid(),
            ReporterId = me,
            TargetType = parsed,
            TargetId = req.TargetId,
            Reason = req.Reason.Trim(),
            Notes = req.Notes?.Trim(),
            Status = ReportStatus.Open
        };

        db.Reports.Add(report);
        await db.SaveChangesAsync(ct);

        return Ok(new ReportDto(report.ReportId, report.ReporterId, report.TargetType.ToString(), report.TargetId, report.Reason, report.Notes, report.Status.ToString(), report.CreatedAt));
    }
}

namespace CommunityOS.Api.DTOs;

public sealed record CreateReportRequest(
    string TargetType,
    Guid TargetId,
    string Reason,
    string? Notes
);

public sealed record ReportDto(
    Guid ReportId,
    Guid ReporterId,
    string TargetType,
    Guid TargetId,
    string Reason,
    string? Notes,
    string Status,
    DateTimeOffset CreatedAt
);

public sealed record ModActionRequest(
    string ActionType,
    string TargetType,
    Guid TargetId,
    string? Notes
);

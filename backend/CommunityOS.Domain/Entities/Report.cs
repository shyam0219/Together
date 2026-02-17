namespace CommunityOS.Domain.Entities;

public sealed class Report : BaseEntity
{
    public Guid ReportId { get; set; }

    public Guid ReporterId { get; set; }
    public User Reporter { get; set; } = null!;

    public ReportTargetType TargetType { get; set; }

    public Guid TargetId { get; set; }

    public string Reason { get; set; } = null!;

    public string? Notes { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Open;
}

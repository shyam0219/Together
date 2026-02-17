namespace CommunityOS.Api.DTOs;

public sealed record PageResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    bool HasMore
);

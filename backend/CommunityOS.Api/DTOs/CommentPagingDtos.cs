namespace CommunityOS.Api.DTOs;

public sealed record PagedCommentsResponse(
    IReadOnlyList<CommentDto> Items,
    int Page,
    int PageSize,
    bool HasMore
);

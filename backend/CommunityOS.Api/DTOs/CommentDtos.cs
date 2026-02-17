using CommunityOS.Domain.Entities;

namespace CommunityOS.Api.DTOs;

public sealed record CommentDto(
    Guid CommentId,
    Guid PostId,
    Guid AuthorId,
    string AuthorName,
    Guid? ParentCommentId,
    string Text,
    DateTimeOffset CreatedAt
);

public sealed record CreateCommentRequest(string Text, Guid? ParentCommentId);

public sealed record UpdateCommentRequest(string Text);

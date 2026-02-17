using CommunityOS.Domain.Entities;

namespace CommunityOS.Api.DTOs;

public sealed record PostImageDto(Guid PostImageId, string Url, int SortOrder);

public sealed record PostDto(
    Guid PostId,
    Guid AuthorId,
    string AuthorName,
    string BodyText,
    string? LinkUrl,
    string? LinkTitle,
    string? LinkDescription,
    string? LinkImageUrl,
    bool CommentingEnabled,
    string Status,
    DateTimeOffset CreatedAt,
    IReadOnlyList<PostImageDto> Images,
    int LikeCount,
    int CommentCount,
    bool LikedByMe,
    bool BookmarkedByMe
);

public sealed record CreatePostRequest(
    string BodyText,
    List<string>? ImageUrls,
    string? LinkUrl,
    string? LinkTitle,
    string? LinkDescription,
    string? LinkImageUrl,
    Guid? GroupId
);

public sealed record UpdatePostRequest(
    string BodyText,
    bool? CommentingEnabled
);

public sealed record AddPostImagesRequest(List<string> ImageUrls);

public static class PostDtoMapper
{
    public static PostImageDto ToDto(PostImage img) => new(img.PostImageId, img.Url, img.SortOrder);
}

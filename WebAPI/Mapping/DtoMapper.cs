using ApiContracts;
using Entities;

namespace WebAPI.Mapping;

// Maps entities to DTOs. The DTOs have no navigation properties, so there are
// no cycles (Post -> User -> Posts -> ...) when they are serialized to JSON.
public static class DtoMapper
{
    public static UserDto ToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserName = user.UserName
        };
    }

    // Expects comment.User to be loaded (Include) for the author name
    public static CommentDto ToDto(Comment comment)
    {
        return new CommentDto
        {
            Id = comment.Id,
            Body = comment.Body,
            PostId = comment.PostId,
            UserId = comment.UserId,
            AuthorName = comment.User?.UserName ?? "(unknown user)"
        };
    }

    // Expects post.User to be loaded (Include), and post.Comments (+ their User) when includeComments is true
    public static PostDto ToDto(Post post, bool includeComments = false)
    {
        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Body = post.Body,
            UserId = post.UserId,
            AuthorName = post.User?.UserName ?? "(unknown user)",
            LikeCount = post.LikedPostIds.Count,
            DislikeCount = post.DislikedPostIds.Count,
            Comments = includeComments
                ? post.Comments.Select(ToDto).ToList()
                : null
        };
    }
}

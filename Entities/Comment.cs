namespace Entities;

public class Comment
{
    public int Id { get; set; }
    public string Body { get; set; } = string.Empty;

    // Foreign key + navigation to the author
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Foreign key + navigation to the post
    public int PostId { get; set; }
    public Post Post { get; set; } = null!;

    public HashSet<int> LikedCommentIds { get; set; } = new();
    public HashSet<int> DislikedCommentIds { get; set; } = new();
}

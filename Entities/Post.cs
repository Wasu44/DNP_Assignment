namespace Entities;

public class Post
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    // Foreign key + navigation to the author (EFC picks this up by naming convention)
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // Navigation to the comments on this post
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public HashSet<int> LikedPostIds { get; set; } = new();
    public HashSet<int> DislikedPostIds { get; set; } = new();
}

using ApiContracts;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryContracts;
using RepositoryContracts.Exceptions;
using WebAPI.Exceptions;
using WebAPI.Mapping;

namespace WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class PostsController : ControllerBase
{
    private readonly IPostRepository postRepo;
    private readonly IUserRepository userRepo;
    private readonly ICommentRepository commentRepo;

    public PostsController(IPostRepository postRepo, IUserRepository userRepo, ICommentRepository commentRepo)
    {
        this.postRepo = postRepo;
        this.userRepo = userRepo;
        this.commentRepo = commentRepo;
    }

    // POST /posts
    [HttpPost]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto request)
    {
        ValidatePost(request.Title, request.Body);

        User? author = await userRepo.GetMany().SingleOrDefaultAsync(u => u.Id == request.UserId);
        if (author is null)
        {
            throw new BadRequestException($"User with ID '{request.UserId}' does not exist");
        }

        Post post = new() { Title = request.Title, Body = request.Body, UserId = request.UserId };
        Post created = await postRepo.AddAsync(post);

        // Returning the entity would fail (Post -> User -> Posts cycle), so return a DTO
        PostDto dto = DtoMapper.ToDto(created);
        dto.AuthorName = author.UserName;
        return Created($"/Posts/{dto.Id}", dto);
    }

    // PUT /posts/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdatePost([FromRoute] int id, [FromBody] UpdatePostDto request)
    {
        ValidatePost(request.Title, request.Body);

        Post existing = await postRepo.GetSingleAsync(id);
        existing.Title = request.Title;
        existing.Body = request.Body;
        await postRepo.UpdateAsync(existing);

        return NoContent();
    }

    // GET /posts/{id}?includeComments=true
    // Loads the post, its author and (optionally) its comments + their authors in ONE database query.
    [HttpGet("{id:int}")]
    public async Task<ActionResult<PostDto>> GetSinglePost([FromRoute] int id, [FromQuery] bool includeComments = false)
    {
        // Step 1: build the query (IQueryable = SQL statement, not executed yet)
        IQueryable<Post> queryForPost = postRepo.GetMany()
            .Where(p => p.Id == id)
            .Include(p => p.User);

        // Step 2: optionally join in the comments and their authors
        if (includeComments)
        {
            queryForPost = queryForPost
                .Include(p => p.Comments)
                .ThenInclude(c => c.User);
        }

        // Step 3: execute the query against the database
        Post? post = await queryForPost.SingleOrDefaultAsync();
        if (post is null)
        {
            throw new NotFoundException($"Post with id {id} not found");
        }

        return Ok(DtoMapper.ToDto(post, includeComments));
    }

    // GET /posts?title=hello&userId=1&userName=anna
    // All filters are translated to SQL and run in the database.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetManyPosts(
        [FromQuery] string? title,
        [FromQuery] int? userId,
        [FromQuery] string? userName)
    {
        IQueryable<Post> query = postRepo.GetMany().Include(p => p.User);

        if (!string.IsNullOrWhiteSpace(title))
        {
            string titleLower = title.ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(titleLower));
        }

        if (userId.HasValue)
        {
            query = query.Where(p => p.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            string userNameLower = userName.ToLower();
            query = query.Where(p => p.User.UserName.ToLower().Contains(userNameLower));
        }

        List<Post> posts = await query.ToListAsync();
        List<PostDto> result = posts.Select(p => DtoMapper.ToDto(p)).ToList();
        return Ok(result);
    }

    // GET /posts/{postId}/comments
    [HttpGet("{postId:int}/comments")]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetCommentsForPost([FromRoute] int postId)
    {
        if (!await postRepo.GetMany().AnyAsync(p => p.Id == postId))
        {
            throw new NotFoundException($"Post with id {postId} not found");
        }

        List<Comment> comments = await commentRepo.GetMany()
            .Where(c => c.PostId == postId)
            .Include(c => c.User)
            .ToListAsync();

        return Ok(comments.Select(DtoMapper.ToDto).ToList());
    }

    // DELETE /posts/{id}
    // The post's comments are deleted by the database (cascade delete, see AppContext).
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeletePost([FromRoute] int id)
    {
        await postRepo.DeleteAsync(id);
        return NoContent();
    }

    private static void ValidatePost(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new BadRequestException("Title cannot be empty");
        if (string.IsNullOrWhiteSpace(body))
            throw new BadRequestException("Body cannot be empty");
    }
}

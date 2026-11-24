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
public class CommentsController : ControllerBase
{
    private readonly ICommentRepository commentRepo;
    private readonly IUserRepository userRepo;
    private readonly IPostRepository postRepo;

    public CommentsController(ICommentRepository commentRepo, IUserRepository userRepo, IPostRepository postRepo)
    {
        this.commentRepo = commentRepo;
        this.userRepo = userRepo;
        this.postRepo = postRepo;
    }

    // POST /comments
    [HttpPost]
    public async Task<ActionResult<CommentDto>> CreateComment([FromBody] CreateCommentDto request)
    {
        ValidateBody(request.Body);

        User? author = await userRepo.GetMany().SingleOrDefaultAsync(u => u.Id == request.UserId);
        if (author is null)
        {
            throw new BadRequestException($"User with ID '{request.UserId}' does not exist");
        }

        if (!await postRepo.GetMany().AnyAsync(p => p.Id == request.PostId))
        {
            throw new BadRequestException($"Post with ID '{request.PostId}' does not exist");
        }

        Comment comment = new() { Body = request.Body, UserId = request.UserId, PostId = request.PostId };
        Comment created = await commentRepo.AddAsync(comment);

        CommentDto dto = DtoMapper.ToDto(created);
        dto.AuthorName = author.UserName;
        return Created($"/Comments/{dto.Id}", dto);
    }

    // PUT /comments/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateComment([FromRoute] int id, [FromBody] UpdateCommentDto request)
    {
        ValidateBody(request.Body);

        Comment existing = await commentRepo.GetSingleAsync(id);
        existing.Body = request.Body;
        await commentRepo.UpdateAsync(existing);

        return NoContent();
    }

    // GET /comments/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CommentDto>> GetSingleComment([FromRoute] int id)
    {
        Comment? comment = await commentRepo.GetMany()
            .Include(c => c.User)
            .SingleOrDefaultAsync(c => c.Id == id);

        if (comment is null)
        {
            throw new NotFoundException($"Comment with id {id} not found");
        }

        return Ok(DtoMapper.ToDto(comment));
    }

    // GET /comments?userId=1&userName=anna&postId=2
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetManyComments(
        [FromQuery] int? userId,
        [FromQuery] string? userName,
        [FromQuery] int? postId)
    {
        IQueryable<Comment> query = commentRepo.GetMany().Include(c => c.User);

        if (userId.HasValue)
        {
            query = query.Where(c => c.UserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(userName))
        {
            string userNameLower = userName.ToLower();
            query = query.Where(c => c.User.UserName.ToLower().Contains(userNameLower));
        }

        if (postId.HasValue)
        {
            query = query.Where(c => c.PostId == postId.Value);
        }

        List<Comment> comments = await query.ToListAsync();
        return Ok(comments.Select(DtoMapper.ToDto).ToList());
    }

    // DELETE /comments/{id}
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteComment([FromRoute] int id)
    {
        await commentRepo.DeleteAsync(id);
        return NoContent();
    }

    private static void ValidateBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new BadRequestException("Comment cannot be empty");
    }
}

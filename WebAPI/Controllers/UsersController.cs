using ApiContracts;
using Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryContracts;
using WebAPI.Exceptions;
using WebAPI.Mapping;

namespace WebAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository userRepo;

    public UsersController(IUserRepository userRepo)
    {
        this.userRepo = userRepo;
    }

    // POST /users
    [HttpPost]
    public async Task<ActionResult<UserDto>> AddUser([FromBody] CreateUserDto request)
    {
        ValidateUser(request.UserName, request.Password);
        await VerifyUserNameIsAvailableAsync(request.UserName);

        User user = new() { UserName = request.UserName, Password = request.Password };
        User created = await userRepo.AddAsync(user);

        UserDto dto = DtoMapper.ToDto(created);
        return Created($"/Users/{dto.Id}", dto);
    }

    // PUT /users/{id}
    [HttpPut("{id:int}")]
    public async Task<ActionResult> UpdateUser([FromRoute] int id, [FromBody] UpdateUserDto request)
    {
        ValidateUser(request.UserName, request.Password);

        User existing = await userRepo.GetSingleAsync(id);

        if (!existing.UserName.Equals(request.UserName, StringComparison.OrdinalIgnoreCase))
        {
            await VerifyUserNameIsAvailableAsync(request.UserName);
        }

        existing.UserName = request.UserName;
        existing.Password = request.Password;
        await userRepo.UpdateAsync(existing);

        return NoContent();
    }

    // GET /users/{id}
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> GetSingleUser([FromRoute] int id)
    {
        User user = await userRepo.GetSingleAsync(id);
        return Ok(DtoMapper.ToDto(user));
    }

    // GET /users?userName=an
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetManyUsers([FromQuery] string? userName)
    {
        IQueryable<User> query = userRepo.GetMany();

        if (!string.IsNullOrWhiteSpace(userName))
        {
            string userNameLower = userName.ToLower();
            query = query.Where(u => u.UserName.ToLower().Contains(userNameLower));
        }

        List<User> users = await query.ToListAsync();
        return Ok(users.Select(DtoMapper.ToDto).ToList());
    }

    // DELETE /users/{id}  (the user's posts and comments are cascade-deleted by the database)
    [HttpDelete("{id:int}")]
    public async Task<ActionResult> DeleteUser([FromRoute] int id)
    {
        await userRepo.DeleteAsync(id);
        return NoContent();
    }

    private async Task VerifyUserNameIsAvailableAsync(string userName)
    {
        string userNameLower = userName.ToLower();
        bool taken = await userRepo.GetMany()
            .AnyAsync(u => u.UserName.ToLower() == userNameLower);

        if (taken)
        {
            throw new BadRequestException($"Username '{userName}' is already taken");
        }
    }

    private static void ValidateUser(string userName, string password)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new BadRequestException("Username cannot be empty");
        if (string.IsNullOrWhiteSpace(password))
            throw new BadRequestException("Password cannot be empty");
    }
}

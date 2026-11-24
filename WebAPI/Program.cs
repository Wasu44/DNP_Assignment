using EfcRepositories;
using RepositoryContracts;
using WebAPI.Middleware;
using AppContext = EfcRepositories.AppContext; // avoid clash with System.AppContext

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Register the DbContext (database is configured in AppContext.OnConfiguring)
builder.Services.AddDbContext<AppContext>();

// Register repository implementations (swap these lines to change persistence)
builder.Services.AddScoped<IUserRepository, EfcUserRepository>();
builder.Services.AddScoped<IPostRepository, EfcPostRepository>();
builder.Services.AddScoped<ICommentRepository, EfcCommentRepository>();

// Global exception handler (optional step 4.6)
builder.Services.AddTransient<GlobalExceptionHandler>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandler>();

app.MapControllers();

app.Run();

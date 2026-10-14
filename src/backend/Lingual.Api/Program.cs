using Lingual.Infrastructure;
using Lingual.Infrastructure.Endpoint;
using Lingual.Infrastructure.RateLimiter;
using Lingual.Modules.Gamification;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure services (Versioning, Swagger, Redis, RateLimiting, Serilog, FluentValidation, MediatR, Exception Handler)
builder.AddInfrastructure(
    typeof(Program).Assembly,
    typeof(GameHub).Assembly
);

// Add services to the container.
builder.Services.AddControllers();

// Realtime SignalR
builder.Services.AddSignalR();

// Health Checks
builder.Services.AddHealthChecks();

// CORS for Next.js Web App & Mezon Client
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline via Infrastructure
// 1. Exception Handler, Serilog Request Logging, Swagger UI
app.UseInfrastructure();

// 2. CORS (đặt trước Auth và RateLimiting để response lỗi hoặc preflight luôn có đủ headers)
app.UseCors("AllowFrontend");

// 3. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 4. Rate Limiting (chạy sau Auth để nhận diện được user ID từ token)
app.UseRateLimiting();

// 5. Endpoint & Hub mapping
app.MapControllers();
app.MapEndpoints();
app.MapHealthChecks("/health");
app.MapHub<GameHub>("/hubs/game");

app.Run();

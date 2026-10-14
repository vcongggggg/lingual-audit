using Lingual.Infrastructure;
using Lingual.Infrastructure.Endpoint;
using Lingual.Infrastructure.RateLimiter;
using Lingual.Modules.AITutor;
using Lingual.Modules.Gamification;
using Lingual.Modules.Identity;
using Lingual.Modules.Learning;
using Lingual.Modules.MezonBot;
using Lingual.Modules.AITutor.Infrastructure.Data;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Identity.Infrastructure.Data;
using Lingual.Modules.Learning.Infrastructure.Data;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add Domain Modules (Database, Persistence & Business Services)
builder.AddIdentityModule();
builder.AddLearningModule();
builder.AddGamificationModule();
builder.AddMezonBotModule();
builder.AddAITutorModule();

// Add Infrastructure services (Versioning, Swagger, Redis, RateLimiting, Serilog, FluentValidation, MediatR, Exception Handler)
builder.AddInfrastructure(
    typeof(Program).Assembly,
    Lingual.Modules.Identity.AssemblyReference.Assembly,
    Lingual.Modules.Learning.AssemblyReference.Assembly,
    Lingual.Modules.Gamification.AssemblyReference.Assembly,
    Lingual.Modules.MezonBot.AssemblyReference.Assembly,
    Lingual.Modules.AITutor.AssemblyReference.Assembly
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

// Database Migration Mode (triggered via command: ["--migrate"] in Docker or CLI)
if (args.Contains("--migrate"))
{
    Console.WriteLine("==========================================================");
    Console.WriteLine("  Lingual Database Auto-Migrator (CLI / Docker)");
    Console.WriteLine("==========================================================");

    var connected = await app.Services.WaitForDatabaseAsync<IdentityDbContext>();
    if (!connected)
    {
        Console.Error.WriteLine("Error: Unable to connect to PostgreSQL database.");
        Environment.Exit(1);
    }

    await app.Services.MigrateDbContextAsync<IdentityDbContext>("IdentityDbContext");
    await app.Services.MigrateDbContextAsync<LearningDbContext>("LearningDbContext");
    await app.Services.MigrateDbContextAsync<GamificationDbContext>("GamificationDbContext");
    await app.Services.MigrateDbContextAsync<MezonBotDbContext>("MezonBotDbContext");
    await app.Services.MigrateDbContextAsync<AITutorDbContext>("AITutorDbContext");

    Console.WriteLine("\n==========================================================");
    Console.WriteLine("  All module migrations applied successfully!");
    Console.WriteLine("==========================================================");
    Environment.Exit(0);
}

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

public partial class Program { }

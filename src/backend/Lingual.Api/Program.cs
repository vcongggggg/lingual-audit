using Lingual.Infrastructure;
using Lingual.Infrastructure.Endpoint;
using Lingual.Infrastructure.RateLimiter;
using Lingual.Modules.AITutor;
using Lingual.Modules.AITutor.Infrastructure.Data;
using Lingual.Modules.Gamification;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Identity;
using Lingual.Modules.Identity.Infrastructure.Data;
using Lingual.Modules.Learning;
using Lingual.Modules.Learning.Infrastructure.Data;
using Lingual.Modules.MezonBot;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Lingual.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Load local overrides if present (gitignored, individual developer settings)
builder.Configuration
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Register domain modules
builder.AddIdentityModule();
builder.AddLearningModule();
builder.AddGamificationModule();
builder.AddMezonBotModule();
builder.AddAITutorModule();

// Register infrastructure services
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
builder.Services.AddEndpointsApiExplorer();

// Realtime SignalR
builder.Services.AddSignalR();

// Health Checks
builder.Services.AddHealthChecks();

// Mezon Bot Module (Mezon.Net.Sdk 1.7.0)
builder.Services.AddMezonBotModule(builder.Configuration);

// AI Tutor Module (Google Gemini API)
builder.Services.AddAITutorModule(builder.Configuration);

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

    return;
}

// Configure the HTTP request pipeline
app.UseInfrastructure();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiting();

app.MapControllers();
app.MapEndpoints();
app.MapHealthChecks("/health");
app.MapHub<GameHub>("/hubs/game");

app.Run();

public partial class Program { }

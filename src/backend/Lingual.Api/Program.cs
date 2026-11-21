using Microsoft.EntityFrameworkCore;
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
    .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

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

    // Đảm bảo xp_ledger.source_type tương thích toàn bộ loại nguồn XP và đảm bảo bảng badges/user_badges tồn tại
    using (var scope = app.Services.CreateScope())
    {
        var gamificationDb = scope.ServiceProvider.GetRequiredService<GamificationDbContext>();
        await gamificationDb.Database.ExecuteSqlRawAsync(@"
            ALTER TABLE IF EXISTS xp_ledger DROP CONSTRAINT IF EXISTS xp_ledger_source_type_check;
            ALTER TABLE IF EXISTS xp_ledger ALTER COLUMN source_type TYPE VARCHAR(32);
            DROP INDEX IF EXISTS uq_xp_ledger_source;
            CREATE UNIQUE INDEX IF NOT EXISTS uq_xp_ledger_source
                ON xp_ledger(user_id, source_type, source_id) WHERE source_id IS NOT NULL AND idempotency_key IS NULL;
            CREATE TABLE IF NOT EXISTS badges (
                id VARCHAR(64) PRIMARY KEY,
                name VARCHAR(120) NOT NULL,
                category VARCHAR(32) NOT NULL DEFAULT 'general',
                rarity VARCHAR(32) NOT NULL DEFAULT 'common',
                icon VARCHAR(64) NOT NULL DEFAULT 'award',
                description TEXT NOT NULL DEFAULT '',
                max_progress INTEGER NOT NULL DEFAULT 1,
                xp_reward INTEGER NOT NULL DEFAULT 50,
                lore TEXT NOT NULL DEFAULT '',
                created_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );
            CREATE TABLE IF NOT EXISTS user_badges (
                user_id UUID NOT NULL,
                badge_id VARCHAR(64) NOT NULL REFERENCES badges(id) ON DELETE CASCADE,
                is_unlocked BOOLEAN NOT NULL DEFAULT FALSE,
                current_progress INTEGER NOT NULL DEFAULT 0,
                unlocked_at TIMESTAMPTZ,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                PRIMARY KEY (user_id, badge_id)
            );");
    }

    Console.WriteLine("\n==========================================================");
    Console.WriteLine("  All module migrations applied successfully!");
    Console.WriteLine("==========================================================");

    return;
}

// Configure the HTTP request pipeline
app.UseInfrastructure();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LINGUAL API v1");
        c.RoutePrefix = string.Empty;
    });
}

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

using EntityFramework.Exceptions.PostgreSQL;
using Lingual.Persistence.Interceptors;
using Lingual.SharedKernel.Events;
using Lingual.SharedKernel.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lingual.Persistence;

public static class Extension
{
    public static IServiceCollection AddAppDbContext<TDbContext>(
        this IServiceCollection services, 
        string connString, 
        IModel? model = null,
        Action<IServiceCollection>? doMoreActions = null)
        where TDbContext : DbContext, IDatabaseFacade, IDomainEventContext

    {
        services.AddSingleton<AuditableEntityInterceptor>();

        services.AddDbContextPool<TDbContext>((sp, options) =>
        {
            options.UseNpgsql(connString, sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(TDbContext).Assembly.FullName);
                    sqlOptions.MigrationsHistoryTable($"__EFMigrationsHistory_{typeof(TDbContext).Name.Replace("DbContext", "")}");
                    sqlOptions.EnableRetryOnFailure(15, TimeSpan.FromSeconds(30),
                        null);
                })
                .UseExceptionProcessor()
                .UseSnakeCaseNamingConvention()
                .EnableServiceProviderCaching()
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);

            if (model is not null)
                options.UseModel(model);

            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == Environments.Development)
                options
                    .EnableDetailedErrors()
                    .EnableSensitiveDataLogging();
        });

        services.AddScoped<IDatabaseFacade>(p => p.GetRequiredService<TDbContext>());
        services.AddScoped<IDomainEventContext>(p => p.GetRequiredService<TDbContext>());

        doMoreActions?.Invoke(services);

        return services;
    }

    public static IServiceCollection AddRepository(this IServiceCollection services, Type type)
    {
        services.Scan(scan => scan
            .FromAssembliesOf(type)
            .AddClasses(classes =>
                classes.AssignableTo(type)).As(typeof(IRepository<>)).WithScopedLifetime()
            .AddClasses(classes =>
                classes.AssignableTo(type)).As(typeof(IReadRepository<>)).WithScopedLifetime()
        );

        return services;
    }

    public static async Task<bool> WaitForDatabaseAsync<TDbContext>(this IServiceProvider serviceProvider, int maxRetries = 15, int delaySeconds = 2)
        where TDbContext : DbContext
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        Console.WriteLine("Connecting to database...");
        for (var i = 1; i <= maxRetries; i++)
        {
            try
            {
                if (await context.Database.CanConnectAsync())
                {
                    Console.WriteLine("Successfully connected to PostgreSQL database!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Attempt {i}/{maxRetries}] Waiting for database... ({ex.Message})");
            }

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
        }

        return false;
    }

    public static async Task MigrateDbContextAsync<TDbContext>(this IServiceProvider serviceProvider, string moduleName)
        where TDbContext : DbContext
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TDbContext>();

        Console.WriteLine($"\n--> Checking migrations for [{moduleName}]...");

        // Nếu DB đã được khởi tạo trước bằng 00_init_all.sql (Docker entrypoint) mà chưa có lịch sử EF,
        // ghi nhận baseline cho các migration 20261014* để tránh lỗi 42P07 (relation already exists)
        // và chỉ chạy các migration mới của Sprint 2 trở đi.
        var applied = (await context.Database.GetAppliedMigrationsAsync()).ToList();
        if (applied.Count == 0)
        {
            var probeTable = typeof(TDbContext).Name switch
            {
                "IdentityDbContext" => "users",
                "LearningDbContext" => "vocabulary_items",
                "GamificationDbContext" => "xp_ledger",
                "MezonBotDbContext" => "quiz_questions",
                "AITutorDbContext" => "ai_scenarios",
                _ => null
            };

            if (probeTable != null)
            {
                var conn = context.Database.GetDbConnection();
                if (conn.State != System.Data.ConnectionState.Open)
                {
                    await conn.OpenAsync();
                }

                await using var probeCmd = conn.CreateCommand();
                probeCmd.CommandText = $"SELECT to_regclass('public.{probeTable}') IS NOT NULL;";
                var exists = Convert.ToBoolean(await probeCmd.ExecuteScalarAsync());

                if (exists)
                {
                    var historyTable = $"__EFMigrationsHistory_{typeof(TDbContext).Name.Replace("DbContext", "")}";
                    var baselineMigrations = context.Database.GetMigrations()
                        .Where(m => m.StartsWith("20261014", StringComparison.Ordinal))
                        .ToList();

                    if (baselineMigrations.Count > 0)
                    {
                        Console.WriteLine($"    Detected pre-initialized SQL schema for [{moduleName}]. Baselining {baselineMigrations.Count} initial migration(s)...");
                        await using var createHist = conn.CreateCommand();
                        createHist.CommandText = $@"
                            CREATE TABLE IF NOT EXISTS ""{historyTable}"" (
                                migration_id character varying(150) NOT NULL,
                                product_version character varying(32) NOT NULL,
                                CONSTRAINT ""pk_{historyTable.ToLowerInvariant()}"" PRIMARY KEY (migration_id)
                            );";
                        await createHist.ExecuteNonQueryAsync();

                        foreach (var mId in baselineMigrations)
                        {
                            await using var insCmd = conn.CreateCommand();
                            insCmd.CommandText = $@"INSERT INTO ""{historyTable}"" (migration_id, product_version) VALUES (@mid, '8.0.11') ON CONFLICT DO NOTHING;";
                            var p = insCmd.CreateParameter();
                            p.ParameterName = "@mid";
                            p.Value = mId;
                            insCmd.Parameters.Add(p);
                            await insCmd.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }

        var pendingMigrations = (await context.Database.GetPendingMigrationsAsync()).ToList();

        if (pendingMigrations.Count == 0)
        {
            Console.WriteLine($"    [{moduleName}] is already up to date. (0 pending)");
            return;
        }

        Console.WriteLine($"    Found {pendingMigrations.Count} pending migration(s) for [{moduleName}]:");
        foreach (var migration in pendingMigrations)
        {
            Console.WriteLine($"      * {migration}");
        }

        Console.WriteLine($"    Applying migrations for [{moduleName}]...");
        await context.Database.MigrateAsync();
        Console.WriteLine($"    Successfully applied migrations for [{moduleName}]!");
    }
}
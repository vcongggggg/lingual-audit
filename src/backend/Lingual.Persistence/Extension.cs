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
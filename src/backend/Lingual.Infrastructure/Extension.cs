using System.Diagnostics;
using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Lingual.Infrastructure.Cache;
using Lingual.Infrastructure.Endpoint;
using Lingual.Infrastructure.Exception;
using Lingual.Infrastructure.Logging;
using Lingual.Infrastructure.RateLimiter;
using Lingual.Infrastructure.Swagger;
using Lingual.Infrastructure.Validator;
using Lingual.Infrastructure.Versioning;
using Serilog;

namespace Lingual.Infrastructure;

public static class Extension
{
    [DebuggerStepThrough]
    public static WebApplicationBuilder AddInfrastructure(
        this WebApplicationBuilder builder,
        params Assembly[] additionalAssemblies)
    {
        var services = builder.Services;

        var assemblies = new List<Assembly>
        {
            typeof(Extension).Assembly
        };

        if (additionalAssemblies is { Length: > 0 })
        {
            assemblies.AddRange(additionalAssemblies);
        }

        var targetAssemblies = assemblies.Distinct().ToArray();

        // 1. API Versioning (required for Swagger ApiExplorer)
        services.AddVersioning();

        // 2. OpenAPI / Swagger documentation
        services.AddOpenApi();

        // 3. ProblemDetails & Global Exception Handling
        services.AddCustomExceptionHandler();

        // 4. Rate Limiting (Sliding Window)
        builder.AddRateLimiting();

        // 5. Distributed Redis Caching
        builder.AddRedisCache();

        // 6. Structured Logging (Serilog)
        builder.AddSerilog();

        // 7. FluentValidation scanning
        services.AddValidator(targetAssemblies);

        // 8. Minimal API Endpoints scanning
        services.AddEndpoints(targetAssemblies);

        // 9. MediatR & Pipeline Behaviors (Logging & Validation)
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(targetAssemblies);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(RequestValidationBehavior<,>));
        });

        return builder;
    }

    [DebuggerStepThrough]
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        WebApplicationBuilder builder,
        params Assembly[] additionalAssemblies)
    {
        builder.AddInfrastructure(additionalAssemblies);
        return services;
    }

    [DebuggerStepThrough]
    public static WebApplication UseInfrastructure(this WebApplication app)
    {
        app.UseCustomExceptionHandler();
        app.UseSerilogRequestLogging();
        app.UseOpenApi();

        return app;
    }
}
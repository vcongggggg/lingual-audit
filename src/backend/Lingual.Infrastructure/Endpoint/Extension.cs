using System.Reflection;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Infrastructure.Endpoint;

public static class Extension
{
    public static IServiceCollection AddEndpoints(this IServiceCollection services, params Assembly[] assemblies)
    {
        var targetAssemblies = assemblies.Length > 0 ? assemblies : [Assembly.GetCallingAssembly()];

        services.Scan(scan => scan
            .FromAssemblies(targetAssemblies)
            .AddClasses(classes => classes.AssignableTo<IEndpointBase>())
            .AsImplementedInterfaces()
            .WithTransientLifetime());

        return services;
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var endpoints = app.ServiceProvider.GetServices<IEndpointBase>();
        foreach (var endpoint in endpoints)
        {
            endpoint.MapEndpoint(app);
        }

        return app;
    }
}


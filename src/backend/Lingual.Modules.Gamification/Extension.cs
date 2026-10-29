using System.Reflection;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Modules.Gamification;

public static class Extension
{
    public static WebApplicationBuilder AddGamificationModule(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // Gamification DbContext & Persistence
        services.AddGamificationDbContext(configuration);

        return builder;
    }

    public static IServiceCollection AddGamificationModule(this WebApplicationBuilder builder, List<Assembly> assemblies)
    {
        builder.AddGamificationModule();
        assemblies.Add(AssemblyReference.Assembly);
        return builder.Services;
    }
}

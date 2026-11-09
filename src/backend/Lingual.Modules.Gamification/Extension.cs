using System.Reflection;
using Lingual.Modules.Gamification.Contracts;
using Lingual.Modules.Gamification.Infrastructure.Data;
using Lingual.Modules.Gamification.Services;
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

        // Core Gamification Services
        services.AddScoped<IStreakService, StreakService>();
        services.AddScoped<ILeaderboardService, LeaderboardService>();
        services.AddScoped<IXpService, XpService>();
        services.AddScoped<IGamificationService, GamificationService>();

        return builder;
    }

    public static IServiceCollection AddGamificationModule(this WebApplicationBuilder builder, List<Assembly> assemblies)
    {
        builder.AddGamificationModule();
        assemblies.Add(AssemblyReference.Assembly);
        return builder.Services;
    }
}

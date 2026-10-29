using System.Reflection;
using Lingual.Modules.MezonBot.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Modules.MezonBot;

public static class Extension
{
    public static WebApplicationBuilder AddMezonBotModule(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // MezonBot DbContext & Persistence
        services.AddMezonBotDbContext(configuration);

        return builder;
    }

    public static IServiceCollection AddMezonBotModule(this WebApplicationBuilder builder, List<Assembly> assemblies)
    {
        builder.AddMezonBotModule();
        assemblies.Add(AssemblyReference.Assembly);
        return builder.Services;
    }
}

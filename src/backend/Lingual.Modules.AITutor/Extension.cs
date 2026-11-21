using System.Reflection;
using Lingual.Modules.AITutor.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Modules.AITutor;

public static class Extension
{
    public static WebApplicationBuilder AddAITutorModule(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // AITutor DbContext & Persistence
        services.AddAITutorDbContext(configuration);

        // AITutor Service & Options
        services.AddAITutorModule(configuration);

        return builder;
    }

    public static IServiceCollection AddAITutorModule(this WebApplicationBuilder builder, List<Assembly> assemblies)
    {
        builder.AddAITutorModule();
        assemblies.Add(AssemblyReference.Assembly);
        return builder.Services;
    }
}

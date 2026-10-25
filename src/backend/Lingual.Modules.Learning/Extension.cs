using System.Reflection;
using Lingual.Modules.Learning.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Modules.Learning;

public static class Extension
{
    public static WebApplicationBuilder AddLearningModule(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // Learning DbContext & Persistence
        services.AddLearningDbContext(configuration);

        return builder;
    }

    public static IServiceCollection AddLearningModule(this WebApplicationBuilder builder, List<Assembly> assemblies)
    {
        builder.AddLearningModule();
        assemblies.Add(AssemblyReference.Assembly);
        return builder.Services;
    }
}

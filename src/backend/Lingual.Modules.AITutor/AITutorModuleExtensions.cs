using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lingual.Modules.AITutor.Contracts;
using Lingual.Modules.AITutor.Options;
using Lingual.Modules.AITutor.Services;

namespace Lingual.Modules.AITutor;

public static class AITutorModuleExtensions
{
    public static IServiceCollection AddAITutorModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));

        services.AddHttpClient<IAITutorService, GeminiAITutorService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}

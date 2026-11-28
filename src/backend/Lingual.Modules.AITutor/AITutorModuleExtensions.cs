using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        services.AddMemoryCache();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IAiQuotaService, AiQuotaService>();

        services.AddHttpClient<IAITutorService, GeminiAITutorService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        return services;
    }
}

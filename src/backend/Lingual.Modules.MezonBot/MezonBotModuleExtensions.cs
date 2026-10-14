using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Modules.MezonBot;

public static class MezonBotModuleExtensions
{
    public static IServiceCollection AddMezonBotModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MezonBotOptions>(configuration.GetSection(MezonBotOptions.SectionName));
        services.AddHostedService<MezonBotWorker>();
        return services;
    }
}

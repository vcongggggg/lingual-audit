using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lingual.Modules.MezonBot.Services;

[assembly: InternalsVisibleTo("Lingual.Modules.MezonBot.Tests")]

namespace Lingual.Modules.MezonBot;

public static class MezonBotModuleExtensions
{
    public static IServiceCollection AddMezonBotModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MezonBotOptions>(configuration.GetSection(MezonBotOptions.SectionName));
        services.Configure<ScheduledQuizOptions>(configuration.GetSection(ScheduledQuizOptions.SectionName));
        services.AddScoped<IClanQuizService, ClanQuizService>();
        services.AddScoped<IScheduledQuizService, ScheduledQuizService>();
        services.AddScoped<IUserStreakService, UserStreakService>();
        services.AddScoped<IMezonUserResolver, MezonUserResolver>();
        services.AddSingleton<IButtonInteractionObserver, ButtonInteractionObserver>();

        services.AddSingleton<MezonBotWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<MezonBotWorker>());
        services.AddSingleton<IMezonBotNotifier>(sp => sp.GetRequiredService<MezonBotWorker>());

        services.AddHostedService<ScheduledQuizBackgroundService>();
        return services;
    }
}

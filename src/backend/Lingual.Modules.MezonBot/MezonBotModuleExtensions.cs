using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Lingual.Modules.MezonBot.Services;

namespace Lingual.Modules.MezonBot;

public static class MezonBotModuleExtensions
{
    public static IServiceCollection AddMezonBotModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MezonBotOptions>(configuration.GetSection(MezonBotOptions.SectionName));
        services.AddSingleton<ReactionBookmarkRateLimiter>();
        services.AddSingleton<DmConversationRateLimiter>();
        services.AddScoped<IClanQuizService, ClanQuizService>();
        services.AddScoped<IReactionQuizService, ReactionQuizService>();
        services.AddScoped<IUserStreakService, UserStreakService>();
        services.AddScoped<IMezonUserResolver, MezonUserResolver>();
        services.AddScoped<ISrsReminderService, SrsReminderService>();
        services.AddSingleton<IVoiceStudyRoomTracker, VoiceStudyRoomTracker>();
        services.AddSingleton<IAiEssayCoachService, AiEssayCoachService>();
        services.AddSingleton<EssayRateLimiter>();

        services.AddSingleton<MezonBotWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<MezonBotWorker>());
        services.AddSingleton<IMezonBotNotifier>(sp => sp.GetRequiredService<MezonBotWorker>());

        services.AddHostedService<SrsReminderBackgroundService>();
        return services;
    }
}

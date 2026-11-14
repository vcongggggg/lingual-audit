using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Lingual.Modules.MezonBot.Services;

[assembly: InternalsVisibleTo("Lingual.Modules.MezonBot.Tests")]

namespace Lingual.Modules.MezonBot;

public static class MezonBotModuleExtensions
{
    public static IServiceCollection AddMezonBotModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MezonBotOptions>(configuration.GetSection(MezonBotOptions.SectionName));
        services.Configure<ScheduledQuizOptions>(configuration.GetSection(ScheduledQuizOptions.SectionName));
        services.AddSingleton<ReactionBookmarkRateLimiter>();
        services.AddSingleton<DmConversationRateLimiter>();
        services.AddScoped<IClanQuizService, ClanQuizService>();
        services.AddScoped<IScheduledQuizService, ScheduledQuizService>();
        services.AddScoped<IReactionQuizService, ReactionQuizService>();
        services.AddScoped<IUserStreakService, UserStreakService>();
        services.AddScoped<IMezonUserResolver, MezonUserResolver>();
        services.AddScoped<ISrsReminderService, SrsReminderService>();
        services.AddSingleton<IVoiceStudyRoomTracker, VoiceStudyRoomTracker>();
        services.AddHttpClient("GeminiEssayClient");
        services.AddSingleton<IAiEssayCoachService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<AiEssayCoachService>>();
            var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var httpClientFactory = sp.GetService<IHttpClientFactory>();
            var httpClient = httpClientFactory?.CreateClient("GeminiEssayClient") ?? new System.Net.Http.HttpClient();
            var apiKey = configuration["Gemini:ApiKey"];
            var baseUrl = configuration["Gemini:BaseUrl"];
            var model = configuration["Gemini:Model"];
            return new AiEssayCoachService(logger, null, scopeFactory, httpClient, apiKey, baseUrl, model);
        });
        services.AddSingleton<EssayRateLimiter>();

        services.AddSingleton<MezonBotWorker>();
        services.AddHostedService(sp => sp.GetRequiredService<MezonBotWorker>());
        services.AddSingleton<IMezonBotNotifier>(sp => sp.GetRequiredService<MezonBotWorker>());

        services.AddHostedService<SrsReminderBackgroundService>();
        services.AddHostedService<ScheduledQuizBackgroundService>();
        return services;
    }
}

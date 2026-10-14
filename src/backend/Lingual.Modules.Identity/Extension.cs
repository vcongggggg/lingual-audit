using System.Reflection;
using Lingual.Modules.Identity.Infrastructure.Auth;
using Lingual.Modules.Identity.Infrastructure.Data;
using Lingual.Modules.Identity.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Modules.Identity;

public static class Extension
{
    public static WebApplicationBuilder AddIdentityModule(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var configuration = builder.Configuration;

        // 1. Identity DbContext & Persistence
        services.AddIdentityDbContext(configuration);

        // 2. Token, Cache & Auth Services
        services.AddMemoryCache();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITokenRevocationService, TokenRevocationService>();

        // 3. Mezon SSO OAuth 2.0 Client
        services.AddHttpClient<IMezonAuthService, MezonAuthService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // 4. JWT Authentication (supports API Bearer & SignalR query string)
        services.AddJwtAuthentication(configuration);

        return builder;
    }

    public static IServiceCollection AddIdentityModule(this WebApplicationBuilder builder, List<Assembly> assemblies)
    {
        builder.AddIdentityModule();
        assemblies.Add(AssemblyReference.Assembly);
        return builder.Services;
    }
}
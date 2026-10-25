using Lingual.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Modules.Learning.Infrastructure.Data;

public static class Extension
{
    public static IServiceCollection AddLearningDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        var connString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DATABASE_URL"]
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' or 'DATABASE_URL' not found.");

        services.AddAppDbContext<LearningDbContext>(connString);
        return services;
    }
}

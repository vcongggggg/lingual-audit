using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lingual.Infrastructure.Exception;

public static class Extension
{
    public static IServiceCollection AddCustomExceptionHandler(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<ExceptionHandler>();
        return services;
    }

    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder app)
        => app.UseExceptionHandler();
}
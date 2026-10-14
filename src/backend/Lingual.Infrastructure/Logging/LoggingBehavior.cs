using System.Diagnostics;
using System.Reflection;
using Ardalis.GuardClauses;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lingual.Infrastructure.Logging;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request);

        const string behavior = nameof(LoggingBehavior<TRequest, TResponse>);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("[{Behavior}] Handling request {RequestName}",
                behavior, typeof(TRequest).Name);
        }

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        logger.LogInformation(
            "[{Behavior}] Handled {RequestName} in {ElapsedMilliseconds} ms",
            behavior,
            typeof(TRequest).Name,
            sw.ElapsedMilliseconds);

        if (sw.ElapsedMilliseconds > 3000)
        {
            logger.LogWarning("[{Behavior}] Long-running request {RequestName} took {ElapsedMilliseconds} ms.",
                behavior, typeof(TRequest).Name, sw.ElapsedMilliseconds);
        }

        return response;
    }
}
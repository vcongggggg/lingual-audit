using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lingual.Infrastructure.RateLimiter;

public static class RateLimitExtensions
{
    public const string POLICY = "PerUserRatelimit";

    /// <summary>
    /// Configures Sliding Window rate limiting (5 req/s per authenticated user or IP address).
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IHostApplicationBuilder builder)
    {
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(POLICY, context =>
            {
                // Partition by authenticated user ID (Mezon/JWT), or fallback to client IP
                var partitionKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                   ?? context.Connection.RemoteIpAddress?.ToString()
                                   ?? "anonymous";

                return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromSeconds(1),
                    SegmentsPerWindow = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
            });

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Max(1, (int)retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status429TooManyRequests,
                    title = "Too Many Requests",
                    detail = "Rate limit exceeded. Maximum 5 requests per second allowed."
                }, cancellationToken: token);
            };
        });

        return builder.Services;
    }

    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder app)
        => app.UseRateLimiter();

    public static IEndpointConventionBuilder RequirePerUserRateLimit(this IEndpointConventionBuilder builder)
        => builder.RequireRateLimiting(POLICY);
}
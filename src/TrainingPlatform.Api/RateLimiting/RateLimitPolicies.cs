using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace TrainingPlatform.Api.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>
    /// Throttles the AI question-generation endpoint. Each call drives a local
    /// LLM, so it is both expensive and a DoS surface: cap it per authenticated
    /// user (falling back to remote IP for anonymous callers).
    /// </summary>
    public const string AiQuestionGeneration = "ai-question-generation";

    private const int PermitsPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(AiQuestionGeneration, httpContext =>
            {
                var partitionKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                   ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                   ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = PermitsPerWindow,
                    Window = Window,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });
        });

        return services;
    }
}

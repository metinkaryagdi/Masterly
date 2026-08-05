using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CodeCraftNet.Api.RateLimiting;

public static class RateLimitPolicies
{
    /// <summary>
    /// Throttles the AI question-generation endpoint. Each call drives a local
    /// LLM, so it is both expensive and a DoS surface: cap it per authenticated
    /// user (falling back to remote IP for anonymous callers).
    /// </summary>
    public const string AiQuestionGeneration = "ai-question-generation";

    /// <summary>
    /// Throttles login/register: both are unauthenticated, so partitioning is by
    /// remote IP to blunt credential-stuffing and brute-force attempts.
    /// </summary>
    public const string Auth = "auth";

    /// <summary>
    /// Throttles code execution and challenge submission endpoints to prevent
    /// runner container resource exhaustion or DoS attacks.
    /// </summary>
    public const string CodeExecution = "code-execution";

    private const int PermitsPerWindow = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private const int AuthPermitsPerWindow = 10;
    private static readonly TimeSpan AuthWindow = TimeSpan.FromMinutes(1);

    private const int CodeExecutionPermitsPerWindow = 12;
    private static readonly TimeSpan CodeExecutionWindow = TimeSpan.FromMinutes(1);

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

            options.AddPolicy(Auth, httpContext =>
            {
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = AuthPermitsPerWindow,
                    Window = AuthWindow,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });

            options.AddPolicy(CodeExecution, httpContext =>
            {
                var partitionKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                   ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                   ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = CodeExecutionPermitsPerWindow,
                    Window = CodeExecutionWindow,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });
        });

        return services;
    }
}

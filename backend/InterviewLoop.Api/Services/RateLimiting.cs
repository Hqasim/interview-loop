using System.Threading.RateLimiting;
using InterviewLoop.Api.Dtos;
using Microsoft.AspNetCore.RateLimiting;

namespace InterviewLoop.Api.Services;

/// <summary>
/// Server-side enforcement for "don't spam the grading endpoint" - the authoritative check,
/// since a client-side cooldown alone can be bypassed by calling the API directly (curl, a
/// second tab, etc). Partitioned per client IP so one user's cooldown doesn't block everyone
/// else sharing the demo.
/// </summary>
public static class RateLimiting
{
    public const string GradingPolicyName = "GradingThrottle";
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(3);

    public static void AddGradingRateLimiter(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(GradingPolicyName, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 1,
                        Window = Window,
                        QueueLimit = 0
                    }));

            options.OnRejected = (context, ct) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                return new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
                    new ErrorResponseDto("You're submitting too quickly - please wait a few seconds and try again."),
                    ct));
            };
        });
    }
}

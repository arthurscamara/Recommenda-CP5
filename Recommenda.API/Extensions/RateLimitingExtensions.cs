using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Recommenda.API.Extensions;

/// <summary>
/// Politicas de rate limit (fixed window, particionadas por IP).
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>POST de escrita: 10 requisicoes por minuto por IP.</summary>
    public const string WritePolicy = "escrita";

    /// <summary>Listagem GET v2: 60 requisicoes por minuto por IP.</summary>
    public const string ReadPolicy = "leitura";

    public const int WritePermitLimit = 10;
    public const int ReadPermitLimit  = 60;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddRecommendaRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(WritePolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit       = WritePermitLimit,
                        Window            = Window,
                        QueueLimit        = 0,
                        AutoReplenishment = true
                    }));

            options.AddPolicy(ReadPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit       = ReadPermitLimit,
                        Window            = Window,
                        QueueLimit        = 0,
                        AutoReplenishment = true
                    }));

            options.OnRejected = async (context, cancellationToken) =>
            {
                var httpContext = context.HttpContext;

                var retryAfterSeconds = (int)Window.TotalSeconds;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

                var logger = httpContext.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Recommenda.RateLimiting");

                logger.LogWarning(
                    "Rate limit excedido em {Method} {Path} para {ClientKey}. RetryAfter={RetryAfterSeconds}s TraceId={TraceId}",
                    httpContext.Request.Method,
                    httpContext.Request.Path.Value,
                    GetClientKey(httpContext),
                    retryAfterSeconds,
                    httpContext.TraceIdentifier);

                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();

                var problem = new ProblemDetails
                {
                    Type     = "https://datatracker.ietf.org/doc/html/rfc6585#section-4",
                    Title    = "Limite de requisicoes excedido",
                    Status   = StatusCodes.Status429TooManyRequests,
                    Detail   = $"Limite de requisicoes atingido para este endpoint. Tente novamente em {retryAfterSeconds} segundos.",
                    Instance = httpContext.Request.Path
                };
                problem.Extensions["retryAfterSeconds"] = retryAfterSeconds;
                problem.Extensions["traceId"]           = httpContext.TraceIdentifier;

                await httpContext.Response.WriteAsJsonAsync(
                    problem,
                    options: null,
                    contentType: "application/problem+json",
                    cancellationToken: cancellationToken);
            };
        });

        return services;
    }

    private static string GetClientKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonimo";
}

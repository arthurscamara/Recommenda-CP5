using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Recommenda.API.HealthChecks;
using Recommenda.Infrastructure.Persistence;

namespace Recommenda.API.Extensions;

/// <summary>
/// Registro e mapeamento dos health checks do Recommenda.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Registra os checks:
    /// <list type="bullet">
    /// <item><c>self</c> — processo no ar (sempre Healthy).</item>
    /// <item><c>mysql</c> — AddDbContextCheck&lt;RecommendaContext&gt; (CanConnectAsync no MySQL).</item>
    /// <item><c>fiap-site</c> — URL externa; falha vira Degraded (nao derruba o /health).</item>
    /// </list>
    /// </summary>
    public static IServiceCollection AddRecommendaHealthChecks(this IServiceCollection services)
    {
        services.AddHttpClient(ExternalUrlHealthCheck.HttpClientName, client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        services.AddHealthChecks()
            .AddCheck(
                "self",
                () => HealthCheckResult.Healthy("API em execucao."),
                tags: ["live"])
            .AddDbContextCheck<RecommendaContext>(
                name: "mysql",
                failureStatus: HealthStatus.Unhealthy,
                tags: ["db", "ready"])
            .AddCheck<ExternalUrlHealthCheck>(
                "fiap-site",
                failureStatus: HealthStatus.Degraded,
                tags: ["external"]);

        return services;
    }

    /// <summary>
    /// Mapeia GET /health com o relatorio completo em JSON.
    /// Healthy -> 200, Degraded -> 200, Unhealthy -> 503. Fora do rate limit.
    /// </summary>
    public static IEndpointRouteBuilder MapRecommendaHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteAsync,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy]   = StatusCodes.Status200OK,
                [HealthStatus.Degraded]  = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        })
        .DisableRateLimiting();

        return endpoints;
    }
}

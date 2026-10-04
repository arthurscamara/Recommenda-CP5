using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Recommenda.API.HealthChecks;

/// <summary>
/// Escreve o relatorio de /health em JSON (status geral, duracao e cada check).
/// Detalhe de excecao so aparece em Development.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented        = true
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        var isDevelopment = context.RequestServices
            .GetRequiredService<IHostEnvironment>()
            .IsDevelopment();

        var payload = new
        {
            status          = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            timestamp       = DateTimeOffset.UtcNow,
            traceId         = context.TraceIdentifier,
            checks = report.Entries.Select(entry => new
            {
                name        = entry.Key,
                status      = entry.Value.Status.ToString(),
                durationMs  = Math.Round(entry.Value.Duration.TotalMilliseconds, 2),
                description = entry.Value.Description,
                tags        = entry.Value.Tags,
                error       = isDevelopment ? entry.Value.Exception?.Message : null
            })
        };

        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}

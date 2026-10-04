using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Recommenda.API.HealthChecks;

/// <summary>
/// Verifica se uma URL externa responde (por padrao, o site da FIAP).
/// Registrado com failureStatus = Degraded: se a URL cair, /health continua 200
/// (a API ainda atende), mas o status agregado vira "Degraded".
/// </summary>
public sealed class ExternalUrlHealthCheck(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IHealthCheck
{
    public const string HttpClientName = "health-external";
    private const string DefaultUrl = "https://www.fiap.com.br";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var url = configuration["HealthChecks:ExternalUrl"] ?? DefaultUrl;

        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"{url} respondeu {(int)response.StatusCode}.")
                : new HealthCheckResult(
                    context.Registration.FailureStatus,
                    $"{url} respondeu {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                $"{url} inacessivel.",
                ex);
        }
    }
}

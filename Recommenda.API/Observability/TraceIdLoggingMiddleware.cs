namespace Recommenda.API.Observability;

/// <summary>
/// Abre um escopo de log com o TraceId da requisicao (HttpContext.TraceIdentifier)
/// e devolve o mesmo valor no header X-Trace-Id. Todo log emitido durante a
/// requisicao (controllers, EF, GlobalExceptionHandler) sai correlacionado.
/// </summary>
public sealed class TraceIdLoggingMiddleware(RequestDelegate next, ILogger<TraceIdLoggingMiddleware> logger)
{
    public const string HeaderName = "X-Trace-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.TraceIdentifier;
        context.Response.Headers[HeaderName] = traceId;

        // Template nomeado: vira propriedade estruturada "TraceId" e texto legivel no console
        using (logger.BeginScope("TraceId:{TraceId}", traceId))
        {
            await next(context);
        }
    }
}

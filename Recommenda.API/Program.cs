using Microsoft.EntityFrameworkCore;
using Recommenda.API.Exceptions;
using Recommenda.API.Extensions;
using Recommenda.API.Observability;
using Recommenda.Infrastructure.Persistence;

namespace Recommenda.API;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // -----------------------------------------------------------------------
        // Logs: console com escopos (TraceId aparece em toda linha da requisicao)
        // -----------------------------------------------------------------------
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes   = true;
            options.SingleLine      = true;
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
        });

        // -----------------------------------------------------------------------
        // Banco de dados
        // -----------------------------------------------------------------------
        builder.Services.AddRecommendaDbContext(builder.Configuration);

        // -----------------------------------------------------------------------
        // Repositorios (generico + especificos) e servicos de aplicacao
        // -----------------------------------------------------------------------
        builder.Services.AddRecommendaRepositories();
        builder.Services.AddRecommendaApplicationServices();

        // -----------------------------------------------------------------------
        // Controllers, versionamento e documentacao
        // -----------------------------------------------------------------------
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddRecommendaApiVersioning();
        builder.Services.AddRecommendaSwagger();

        // -----------------------------------------------------------------------
        // Health checks (self + MySQL + URL externa) e rate limit
        // -----------------------------------------------------------------------
        builder.Services.AddRecommendaHealthChecks();
        builder.Services.AddRecommendaRateLimiting();

        // -----------------------------------------------------------------------
        // Tratamento global de excecoes (RFC 7807 — ProblemDetails)
        // -----------------------------------------------------------------------
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        // Escopo de log com TraceId vem primeiro para cobrir inclusive o exception handler.
        app.UseMiddleware<TraceIdLoggingMiddleware>();

        // UseExceptionHandler deve vir antes de qualquer middleware que possa
        // lancar excecoes, incluindo o Swagger e o MapControllers.
        app.UseExceptionHandler();

        // Aplica migrations pendentes ao iniciar. Se o banco estiver fora do ar a API
        // sobe mesmo assim e o GET /health reporta Unhealthy (503).
        ApplyMigrations(app);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                var provider = app.Services.GetRequiredService<Asp.Versioning.ApiExplorer.IApiVersionDescriptionProvider>();

                // Versao mais nova primeiro (v2.0 abre por padrao)
                foreach (var description in provider.ApiVersionDescriptions.OrderByDescending(d => d.ApiVersion))
                {
                    var name = description.GroupName.ToUpperInvariant();
                    if (description.IsDeprecated)
                        name += " (DEPRECADA)";

                    options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", name);
                }

                options.RoutePrefix = "swagger";
            });
        }

        app.UseHttpsRedirection();

        // Depois do UseExceptionHandler e antes do MapControllers
        app.UseRateLimiter();

        app.UseAuthorization();

        app.MapRecommendaHealthChecks();
        app.MapControllers();

        app.Run();
    }

    private static void ApplyMigrations(WebApplication app)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<RecommendaContext>();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            app.Logger.LogError(
                ex,
                "Nao foi possivel aplicar as migrations no startup. A API seguira no ar e o /health indicara o banco como Unhealthy.");
        }
    }
}

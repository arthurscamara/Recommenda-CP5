using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Recommenda.API.Swagger;

/// <summary>
/// Cria um documento Swagger por versao descoberta pelo ApiExplorer versionado
/// (v1.0 deprecada e v2.0 atual).
/// </summary>
public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfo(description));
        }
    }

    private static OpenApiInfo CreateInfo(ApiVersionDescription description)
    {
        var text =
            "API REST para descoberta musical: gerenciamento de artistas, albuns, " +
            "faixas, generos e avaliacoes de usuarios.";

        if (description.IsDeprecated)
        {
            text +=
                " ATENCAO: esta versao (1.0) esta DEPRECADA. A listagem GET /api/album devolve " +
                "o array completo (contrato antigo) e sera removida no futuro. Migre para a 2.0, " +
                "que devolve o envelope paginado.";
        }
        else
        {
            text +=
                " Versao atual. GET /api/album devolve envelope paginado " +
                "(page, pageSize, totalItems, totalPages, items).";
        }

        return new OpenApiInfo
        {
            Title       = description.IsDeprecated ? "Recommenda API (DEPRECADA)" : "Recommenda API",
            Version     = description.ApiVersion.ToString(),
            Description = text,
            Contact = new OpenApiContact
            {
                Name  = "Equipe Recommenda",
                Email = "contato@recommenda.example.com"
            }
        };
    }
}

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Recommenda.API.Swagger;

/// <summary>
/// Marca operacoes de versoes deprecadas e preenche valores padrao dos
/// parametros de versao (api-version / X-Api-Version) na UI do Swagger.
/// </summary>
public sealed class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        if (operation.Parameters is null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions
                .FirstOrDefault(p => string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

            if (description is null)
                continue;

            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema is not null &&
                parameter.Schema.Default is null &&
                description.DefaultValue is not null &&
                description.DefaultValue is not DBNull &&
                description.ModelMetadata is ModelMetadata modelMetadata)
            {
                var json = JsonSerializer.Serialize(description.DefaultValue, modelMetadata.ModelType);
                parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
            }

            parameter.Required |= description.IsRequired;
        }
    }
}

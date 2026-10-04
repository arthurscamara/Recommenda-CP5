using System.Reflection;
using Asp.Versioning;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Recommenda.API.Swagger;
using Recommenda.Application.Repositories;
using Recommenda.Application.Services;
using Recommenda.Infrastructure;
using Recommenda.Infrastructure.Persistence;
using Recommenda.Infrastructure.Persistence.Repositories;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Recommenda.API.Extensions;

/// <summary>
/// Extensoes para registrar servicos, repositorios e documentacao da solucao Recommenda na DI.
/// </summary>
public static class RecommendaServiceCollectionExtensions
{
    /// <summary>
    /// Registra o <see cref="RecommendaContext"/> com MySQL.
    /// A connection string e lida de <c>ConnectionStrings:RecommendaMySQL</c>.
    /// A versao do servidor vem de <c>Database:MySqlVersion</c> (padrao 8.0.36) em vez de
    /// ServerVersion.AutoDetect, que abre conexao no startup e derrubaria a API com o banco
    /// fora do ar — impedindo o /health de responder 503.
    /// </summary>
    public static IServiceCollection AddRecommendaDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("RecommendaMySQL")
            ?? throw new InvalidOperationException(
                "Connection string 'RecommendaMySQL' nao encontrada. Configure em appsettings.json.");

        var mySqlVersion = Version.Parse(configuration["Database:MySqlVersion"] ?? "8.0.36");

        services.AddDbContext<RecommendaContext>(options =>
            options.UseMySql(connectionString, new MySqlServerVersion(mySqlVersion)));

        return services;
    }

    /// <summary>
    /// Registra o repositorio generico <see cref="IRepository{T}"/> e todos os repositorios
    /// especificos por agregado como <c>Scoped</c> (um por requisicao HTTP).
    /// </summary>
    public static IServiceCollection AddRecommendaRepositories(this IServiceCollection services)
    {
        // Repositorio generico — cobre CRUD basico de qualquer entidade de dominio
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Repositorios especificos com consultas alem do CRUD minimo
        services.AddScoped<IArtistRepository,      ArtistRepository>();
        services.AddScoped<IAlbumRepository,       AlbumRepository>();
        services.AddScoped<ITrackRepository,       TrackRepository>();
        services.AddScoped<IGenreRepository,       GenreRepository>();
        services.AddScoped<IUserRepository,        UserRepository>();
        services.AddScoped<IAlbumRatingRepository, AlbumRatingRepository>();
        services.AddScoped<ITrackRatingRepository, TrackRatingRepository>();
        services.AddScoped<IPlaylistRepository,    PlaylistRepository>();

        return services;
    }

    /// <summary>
    /// Registra os servicos de aplicacao (casos de uso). Os mesmos servicos atendem
    /// as versoes 1.0 e 2.0 da API.
    /// </summary>
    public static IServiceCollection AddRecommendaApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAlbumService,       AlbumService>();
        services.AddScoped<IAlbumRatingService, AlbumRatingService>();

        return services;
    }

    /// <summary>
    /// Versionamento da API: padrao 2.0, versao lida por query (<c>api-version</c>)
    /// ou header (<c>X-Api-Version</c>); sem versao na requisicao cai na 2.0.
    /// </summary>
    public static IServiceCollection AddRecommendaApiVersioning(this IServiceCollection services)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion                   = new ApiVersion(2, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions                   = true;
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new QueryStringApiVersionReader("api-version"),
                    new HeaderApiVersionReader("X-Api-Version"));
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                // Grupos "v1.0" e "v2.0"
                options.GroupNameFormat = "'v'VVVV";
            });

        return services;
    }

    /// <summary>
    /// Configura o Swagger/OpenAPI: um documento por versao da API
    /// (ver <see cref="ConfigureSwaggerOptions"/>) e comentarios XML.
    /// </summary>
    public static IServiceCollection AddRecommendaSwagger(this IServiceCollection services)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

        services.AddSwaggerGen(options =>
        {
            options.OperationFilter<SwaggerDefaultValues>();

            // Comentarios XML dos controllers refletidos na UI do Swagger
            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
        });

        return services;
    }
}

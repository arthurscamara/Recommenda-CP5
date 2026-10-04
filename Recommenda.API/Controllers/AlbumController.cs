using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Recommenda.API.Extensions;
using Recommenda.Application.Common;
using Recommenda.Application.DTOs;
using Recommenda.Application.Services;
using Recommenda.Domain.Exceptions;

namespace Recommenda.API.Controllers;

/// <summary>
/// Gerenciamento de albuns de estudio, EPs e coletaneas.
/// Recurso versionado: 1.0 (deprecada, lista completa) e 2.0 (atual, lista paginada).
/// As duas versoes usam o mesmo <see cref="IAlbumService"/>.
/// </summary>
[ApiController]
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/[controller]")]
[Produces("application/json")]
public class AlbumController(
    IAlbumService albumService,
    ILogger<AlbumController> logger) : ControllerBase
{
    /// <summary>
    /// [v1 — DEPRECADA] Lista todos os albuns cadastrados (array completo, contrato do CP3).
    /// </summary>
    /// <returns>Lista de albuns.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<AlbumResponse>), StatusCodes.Status200OK)]
    public IActionResult GetAllV1()
    {
        return Ok(albumService.GetAll());
    }

    /// <summary>
    /// [v2] Lista albuns paginados, ordenados por titulo.
    /// </summary>
    /// <param name="page">Pagina (base 1). Padrao 1.</param>
    /// <param name="pageSize">Itens por pagina, de 1 a 100. Padrao 20.</param>
    /// <returns>Envelope com page, pageSize, totalItems, totalPages e items.</returns>
    [HttpGet]
    [MapToApiVersion("2.0")]
    [EnableRateLimiting(RateLimitingExtensions.ReadPolicy)]
    [ProducesResponseType(typeof(PagedResponse<AlbumResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public IActionResult GetAllV2(
        [FromQuery] int page = PageRequest.DefaultPage,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize)
    {
        return Ok(albumService.GetPaged(page, pageSize));
    }

    /// <summary>
    /// Busca um album pelo identificador unico.
    /// </summary>
    /// <param name="id">Identificador do album.</param>
    /// <returns>Dados do album encontrado.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AlbumResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        return Ok(albumService.GetById(id));
    }

    /// <summary>
    /// Lista todos os albuns de um artista especifico.
    /// </summary>
    /// <param name="artistId">Identificador do artista.</param>
    /// <returns>Lista de albuns do artista.</returns>
    [HttpGet("artist/{artistId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AlbumResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetByArtist(Guid artistId)
    {
        return Ok(albumService.GetByArtist(artistId));
    }

    /// <summary>
    /// Cria um novo album para um artista existente. Limite: 10 requisicoes por minuto por IP.
    /// </summary>
    /// <remarks>
    /// Exemplo de corpo:
    ///
    ///     POST /api/album
    ///     {
    ///         "title": "No na Orelha",
    ///         "releaseDate": "2011-07-12T00:00:00",
    ///         "artistId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "coverUrl": "https://example.com/capa.jpg"
    ///     }
    ///
    /// </remarks>
    /// <param name="request">Dados do album a ser criado.</param>
    /// <returns>Album criado com seu identificador gerado.</returns>
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(AlbumResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public IActionResult Create([FromBody] AlbumRequest request)
    {
        var traceId = HttpContext.TraceIdentifier;

        logger.LogInformation(
            "Iniciando criacao de album {AlbumTitle} para o artista {ArtistId}. TraceId={TraceId}",
            request.Title, request.ArtistId, traceId);

        AlbumResponse album;
        try
        {
            album = albumService.Create(request);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(
                "Falha de negocio ao criar album {AlbumTitle} para o artista {ArtistId}: {Reason}. TraceId={TraceId}",
                request.Title, request.ArtistId, ex.Message, traceId);
            throw;
        }

        logger.LogInformation(
            "Album {AlbumId} criado com sucesso para o artista {ArtistId}. TraceId={TraceId}",
            album.Id, album.ArtistId, traceId);

        return CreatedAtAction(nameof(GetById), new { id = album.Id }, album);
    }

    /// <summary>
    /// Remove um album pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do album a ser removido.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        albumService.Delete(id);
        return NoContent();
    }
}

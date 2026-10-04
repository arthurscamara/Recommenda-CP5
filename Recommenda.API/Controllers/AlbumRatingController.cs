using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Recommenda.API.Extensions;
using Recommenda.Application.DTOs;
using Recommenda.Application.Services;
using Recommenda.Domain.Exceptions;

namespace Recommenda.API.Controllers;

/// <summary>
/// Gerenciamento de avaliacoes de albuns por usuarios.
/// </summary>
[ApiController]
[ApiVersionNeutral]
[Route("api/[controller]")]
[Produces("application/json")]
public class AlbumRatingController(
    IAlbumRatingService ratingService,
    ILogger<AlbumRatingController> logger) : ControllerBase
{
    /// <summary>
    /// Lista todas as avaliacoes de um album especifico.
    /// </summary>
    /// <param name="albumId">Identificador do album.</param>
    /// <returns>Lista de avaliacoes do album.</returns>
    [HttpGet("album/{albumId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AlbumRatingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetByAlbum(Guid albumId)
    {
        return Ok(ratingService.GetByAlbum(albumId));
    }

    /// <summary>
    /// Lista todas as avaliacoes feitas por um usuario especifico.
    /// </summary>
    /// <param name="userId">Identificador do usuario.</param>
    /// <returns>Lista de avaliacoes do usuario.</returns>
    [HttpGet("user/{userId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AlbumRatingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetByUser(Guid userId)
    {
        return Ok(ratingService.GetByUser(userId));
    }

    /// <summary>
    /// Registra a avaliacao de um usuario sobre um album. Limite: 10 requisicoes por minuto por IP.
    /// </summary>
    /// <remarks>
    /// Cada usuario pode avaliar um album apenas uma vez. Score deve estar entre 1 e 5.
    ///
    ///     POST /api/albumrating
    ///     {
    ///         "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "albumId": "7fa85f64-5717-4562-b3fc-2c963f66afa6",
    ///         "score": 5,
    ///         "comment": "Album incrivel!"
    ///     }
    ///
    /// </remarks>
    /// <param name="request">Dados da avaliacao.</param>
    /// <returns>Avaliacao criada.</returns>
    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.WritePolicy)]
    [ProducesResponseType(typeof(AlbumRatingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public IActionResult Create([FromBody] AlbumRatingRequest request)
    {
        var traceId = HttpContext.TraceIdentifier;

        logger.LogInformation(
            "Iniciando avaliacao do album {AlbumId} pelo usuario {UserId} com nota {Score}. TraceId={TraceId}",
            request.AlbumId, request.UserId, request.Score, traceId);

        AlbumRatingResponse rating;
        try
        {
            rating = ratingService.Create(request);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(
                "Falha de negocio ao avaliar album {AlbumId} pelo usuario {UserId}: {Reason}. TraceId={TraceId}",
                request.AlbumId, request.UserId, ex.Message, traceId);
            throw;
        }

        logger.LogInformation(
            "Avaliacao {RatingId} registrada para o album {AlbumId}. TraceId={TraceId}",
            rating.Id, rating.AlbumId, traceId);

        return StatusCode(StatusCodes.Status201Created, rating);
    }
}

using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Recommenda.Application.DTOs;
using Recommenda.Application.Services;
using Recommenda.Domain.Exceptions;

namespace Recommenda.API.Controllers;

/// <summary>
/// Gerenciamento de faixas musicais dentro de albuns.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[ApiVersionNeutral]
[Produces("application/json")]
public class TrackController(
    ITrackRepository trackRepository,
    IAlbumRepository albumRepository) : ControllerBase
{
    /// <summary>
    /// Lista todas as faixas de um album especifico, ordenadas pelo numero da faixa.
    /// </summary>
    /// <param name="albumId">Identificador do album.</param>
    /// <returns>Lista de faixas do album.</returns>
    [HttpGet("album/{albumId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TrackResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetByAlbum(Guid albumId)
    {
        if (albumRepository.GetById(albumId) is null)
            throw new ResourceNotFoundException("Album", albumId);

        var tracks = trackRepository.GetByAlbum(albumId).Select(TrackResponse.FromDomain);
        return Ok(tracks);
    }

    /// <summary>
    /// Busca uma faixa pelo identificador unico.
    /// </summary>
    /// <param name="id">Identificador da faixa.</param>
    /// <returns>Dados da faixa encontrada.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TrackResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var track = trackRepository.GetById(id);
        if (track is null)
            throw new ResourceNotFoundException("Faixa", id);

        return Ok(TrackResponse.FromDomain(track));
    }

    /// <summary>
    /// Adiciona uma nova faixa a um album existente.
    /// </summary>
    /// <remarks>
    /// Exemplo de corpo:
    ///
    ///     POST /api/track
    ///     {
    ///         "title": "Subirubirundum",
    ///         "durationSeconds": 214,
    ///         "trackNumber": 1,
    ///         "albumId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    ///     }
    ///
    /// </remarks>
    /// <param name="request">Dados da faixa a ser criada.</param>
    /// <returns>Faixa criada com seu identificador gerado.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(TrackResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Create([FromBody] TrackRequest request)
    {
        if (albumRepository.GetById(request.AlbumId) is null)
            throw new ResourceNotFoundException("Album", request.AlbumId);

        var track = trackRepository.Create(request.ToDomain());
        return CreatedAtAction(nameof(GetById), new { id = track.Id }, TrackResponse.FromDomain(track));
    }

    /// <summary>
    /// Remove uma faixa pelo identificador.
    /// </summary>
    /// <param name="id">Identificador da faixa a ser removida.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        if (!trackRepository.Delete(id))
            throw new ResourceNotFoundException("Faixa", id);

        return NoContent();
    }
}

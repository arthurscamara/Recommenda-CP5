using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Recommenda.Application.DTOs;
using Recommenda.Application.Services;
using Recommenda.Domain.Exceptions;

namespace Recommenda.API.Controllers;

/// <summary>
/// Gerenciamento de artistas e bandas musicais.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[ApiVersionNeutral]
[Produces("application/json")]
public class ArtistController(IArtistRepository artistRepository) : ControllerBase
{
    /// <summary>
    /// Lista todos os artistas cadastrados.
    /// </summary>
    /// <returns>Lista de artistas.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ArtistResponse>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var artists = artistRepository.GetAll().Select(ArtistResponse.FromDomain);
        return Ok(artists);
    }

    /// <summary>
    /// Busca um artista pelo identificador unico.
    /// </summary>
    /// <param name="id">Identificador do artista.</param>
    /// <returns>Dados do artista encontrado.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ArtistResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var artist = artistRepository.GetById(id);
        if (artist is null)
            throw new ResourceNotFoundException("Artista", id);

        return Ok(ArtistResponse.FromDomain(artist));
    }

    /// <summary>
    /// Cria um novo artista ou banda.
    /// </summary>
    /// <remarks>
    /// Exemplo de corpo:
    ///
    ///     POST /api/artist
    ///     {
    ///         "name": "Criolo",
    ///         "bio": "Rapper e cantor brasileiro.",
    ///         "country": "Brasil"
    ///     }
    ///
    /// </remarks>
    /// <param name="request">Dados do artista a ser criado.</param>
    /// <returns>Artista criado com seu identificador gerado.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ArtistResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] ArtistRequest request)
    {
        var artist = artistRepository.Create(request.ToDomain());
        return CreatedAtAction(nameof(GetById), new { id = artist.Id }, ArtistResponse.FromDomain(artist));
    }

    /// <summary>
    /// Remove um artista pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do artista a ser removido.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        if (!artistRepository.Delete(id))
            throw new ResourceNotFoundException("Artista", id);

        return NoContent();
    }
}

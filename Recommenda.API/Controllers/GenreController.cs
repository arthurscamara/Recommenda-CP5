using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Recommenda.Application.DTOs;
using Recommenda.Application.Repositories;
using Recommenda.Domain.Entities;
using Recommenda.Domain.Exceptions;

namespace Recommenda.API.Controllers;

/// <summary>
/// Gerenciamento de generos musicais (Rock, Pop, Jazz, etc.).
/// Este recurso utiliza o repositorio generico <c>IRepository&lt;Genre&gt;</c>.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[ApiVersionNeutral]
[Produces("application/json")]
public class GenreController(IRepository<Genre> genreRepository) : ControllerBase
{
    /// <summary>
    /// Lista todos os generos cadastrados.
    /// </summary>
    /// <returns>Lista de generos musicais.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<GenreResponse>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var genres = genreRepository.GetAll().Select(GenreResponse.FromDomain);
        return Ok(genres);
    }

    /// <summary>
    /// Busca um genero pelo identificador unico.
    /// </summary>
    /// <param name="id">Identificador do genero.</param>
    /// <returns>Dados do genero encontrado.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GenreResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetById(Guid id)
    {
        var genre = genreRepository.GetById(id);
        if (genre is null)
            throw new ResourceNotFoundException("Genero", id);

        return Ok(GenreResponse.FromDomain(genre));
    }

    /// <summary>
    /// Cria um novo genero musical.
    /// </summary>
    /// <remarks>
    /// Exemplo de corpo:
    ///
    ///     POST /api/genre
    ///     {
    ///         "name": "MPB",
    ///         "description": "Musica Popular Brasileira."
    ///     }
    ///
    /// </remarks>
    /// <param name="request">Dados do genero a ser criado.</param>
    /// <returns>Genero criado com seu identificador gerado.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(GenreResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] GenreRequest request)
    {
        var genre = genreRepository.Add(request.ToDomain());
        return CreatedAtAction(nameof(GetById), new { id = genre.Id }, GenreResponse.FromDomain(genre));
    }

    /// <summary>
    /// Remove um genero pelo identificador.
    /// </summary>
    /// <param name="id">Identificador do genero a ser removido.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult Delete(Guid id)
    {
        if (!genreRepository.Delete(id))
            throw new ResourceNotFoundException("Genero", id);

        return NoContent();
    }
}

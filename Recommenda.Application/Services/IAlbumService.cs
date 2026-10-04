using Recommenda.Application.DTOs;

namespace Recommenda.Application.Services;

/// <summary>
/// Casos de uso de albuns. Compartilhado pelas versoes 1.0 e 2.0 da API.
/// </summary>
public interface IAlbumService
{
    /// <summary>Lista completa (contrato da v1).</summary>
    IReadOnlyList<AlbumResponse> GetAll();

    /// <summary>Lista paginada (contrato da v2).</summary>
    /// <exception cref="Common.InvalidPageRequestException">page/pageSize fora da faixa.</exception>
    PagedResponse<AlbumResponse> GetPaged(int page, int pageSize);

    AlbumResponse GetById(Guid id);
    IReadOnlyList<AlbumResponse> GetByArtist(Guid artistId);
    AlbumResponse Create(AlbumRequest request);
    void Delete(Guid id);
}

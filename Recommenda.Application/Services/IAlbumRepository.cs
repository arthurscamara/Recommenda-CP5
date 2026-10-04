using Recommenda.Application.Common;
using Recommenda.Domain.Entities;

namespace Recommenda.Application.Services;

/// <summary>Define operações de repositório para álbuns.</summary>
public interface IAlbumRepository
{
    IReadOnlyList<Album> GetAll();

    /// <summary>
    /// Pagina de albuns ordenada por titulo (desempate por Id).
    /// Count + OrderBy + Skip + Take executados no IQueryable.
    /// </summary>
    PagedResult<Album> GetPaged(int page, int pageSize);

    Album? GetById(Guid id);
    IReadOnlyList<Album> GetByArtist(Guid artistId);
    Album Create(Album album);
    bool Delete(Guid id);
}

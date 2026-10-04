using Recommenda.Application.Common;
using Recommenda.Application.DTOs;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Application.Services;

/// <summary>
/// Implementacao dos casos de uso de albuns.
/// Depende apenas das interfaces de repositorio (sem EF, sem HTTP).
/// </summary>
public sealed class AlbumService(
    IAlbumRepository albumRepository,
    IArtistRepository artistRepository) : IAlbumService
{
    public IReadOnlyList<AlbumResponse> GetAll() =>
        albumRepository.GetAll().Select(AlbumResponse.FromDomain).ToList();

    public PagedResponse<AlbumResponse> GetPaged(int page, int pageSize)
    {
        var request = PageRequest.Create(page, pageSize);

        var result = albumRepository.GetPaged(request.Page, request.PageSize);
        var items  = result.Items.Select(AlbumResponse.FromDomain).ToList();

        return PagedResponse<AlbumResponse>.Create(items, request.Page, request.PageSize, result.TotalItems);
    }

    public AlbumResponse GetById(Guid id)
    {
        var album = albumRepository.GetById(id)
            ?? throw new ResourceNotFoundException("Album", id);

        return AlbumResponse.FromDomain(album);
    }

    public IReadOnlyList<AlbumResponse> GetByArtist(Guid artistId)
    {
        if (artistRepository.GetById(artistId) is null)
            throw new ResourceNotFoundException("Artista", artistId);

        return albumRepository.GetByArtist(artistId).Select(AlbumResponse.FromDomain).ToList();
    }

    public AlbumResponse Create(AlbumRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (artistRepository.GetById(request.ArtistId) is null)
            throw new ResourceNotFoundException("Artista", request.ArtistId);

        var album = albumRepository.Create(request.ToDomain());
        return AlbumResponse.FromDomain(album);
    }

    public void Delete(Guid id)
    {
        if (!albumRepository.Delete(id))
            throw new ResourceNotFoundException("Album", id);
    }
}

using Recommenda.Application.DTOs;

namespace Recommenda.Application.Services;

/// <summary>Casos de uso de avaliacoes de albuns.</summary>
public interface IAlbumRatingService
{
    IReadOnlyList<AlbumRatingResponse> GetByAlbum(Guid albumId);
    IReadOnlyList<AlbumRatingResponse> GetByUser(Guid userId);
    AlbumRatingResponse Create(AlbumRatingRequest request);
}

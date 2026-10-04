using Recommenda.Application.DTOs;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Application.Services;

/// <summary>
/// Implementacao dos casos de uso de avaliacoes de albuns.
/// Regras: usuario e album precisam existir; um usuario avalia um album uma unica vez.
/// </summary>
public sealed class AlbumRatingService(
    IAlbumRatingRepository ratingRepository,
    IAlbumRepository albumRepository,
    IUserRepository userRepository) : IAlbumRatingService
{
    public IReadOnlyList<AlbumRatingResponse> GetByAlbum(Guid albumId)
    {
        if (albumRepository.GetById(albumId) is null)
            throw new ResourceNotFoundException("Album", albumId);

        return ratingRepository.GetByAlbum(albumId).Select(AlbumRatingResponse.FromDomain).ToList();
    }

    public IReadOnlyList<AlbumRatingResponse> GetByUser(Guid userId)
    {
        if (userRepository.GetById(userId) is null)
            throw new ResourceNotFoundException("Usuario", userId);

        return ratingRepository.GetByUser(userId).Select(AlbumRatingResponse.FromDomain).ToList();
    }

    public AlbumRatingResponse Create(AlbumRatingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (userRepository.GetById(request.UserId) is null)
            throw new ResourceNotFoundException("Usuario", request.UserId);

        if (albumRepository.GetById(request.AlbumId) is null)
            throw new ResourceNotFoundException("Album", request.AlbumId);

        if (ratingRepository.Exists(request.UserId, request.AlbumId))
            throw new ConflictException("Usuario ja avaliou este album.");

        var rating = ratingRepository.Create(request.ToDomain());
        return AlbumRatingResponse.FromDomain(rating);
    }
}

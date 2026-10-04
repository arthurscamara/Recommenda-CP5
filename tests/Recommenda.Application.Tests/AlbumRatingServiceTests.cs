using Moq;
using Recommenda.Application.DTOs;
using Recommenda.Application.Services;
using Recommenda.Domain.Entities;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Application.Tests;

/// <summary>
/// Testes do AlbumRatingService com mocks dos repositorios (sem banco, sem API).
/// </summary>
public class AlbumRatingServiceTests
{
    private readonly Mock<IAlbumRatingRepository> _ratingRepository = new();
    private readonly Mock<IAlbumRepository>       _albumRepository  = new();
    private readonly Mock<IUserRepository>        _userRepository   = new();
    private readonly AlbumRatingService           _service;

    public AlbumRatingServiceTests()
    {
        _service = new AlbumRatingService(
            _ratingRepository.Object,
            _albumRepository.Object,
            _userRepository.Object);
    }

    private static User NewUser() =>
        new("Arthur", "arthur@example.com", new DateOnly(2000, 1, 1), "senhaForte123");

    private static Album NewAlbum() =>
        new("Convoque Seu Buda", new DateTime(2014, 11, 4), Guid.NewGuid());

    [Fact]
    public void Create_UsuarioInexistente_LancaResourceNotFoundExceptionENaoPersiste()
    {
        // Arrange
        var request = new AlbumRatingRequest(Guid.NewGuid(), Guid.NewGuid(), 5, "Top");
        _userRepository.Setup(r => r.GetById(request.UserId)).Returns((User?)null);

        // Act
        var act = () => _service.Create(request);

        // Assert
        var exception = Assert.Throws<ResourceNotFoundException>(act);
        Assert.Contains(request.UserId.ToString(), exception.Message);
        _ratingRepository.Verify(r => r.Create(It.IsAny<AlbumRating>()), Times.Never);
    }

    [Fact]
    public void Create_AlbumInexistente_LancaResourceNotFoundExceptionENaoPersiste()
    {
        // Arrange
        var request = new AlbumRatingRequest(Guid.NewGuid(), Guid.NewGuid(), 4);
        _userRepository.Setup(r => r.GetById(request.UserId)).Returns(NewUser());
        _albumRepository.Setup(r => r.GetById(request.AlbumId)).Returns((Album?)null);

        // Act
        var act = () => _service.Create(request);

        // Assert
        Assert.Throws<ResourceNotFoundException>(act);
        _ratingRepository.Verify(r => r.Create(It.IsAny<AlbumRating>()), Times.Never);
    }

    [Fact]
    public void Create_UsuarioJaAvaliouAlbum_LancaConflictExceptionENaoPersiste()
    {
        // Arrange
        var request = new AlbumRatingRequest(Guid.NewGuid(), Guid.NewGuid(), 3);
        _userRepository.Setup(r => r.GetById(request.UserId)).Returns(NewUser());
        _albumRepository.Setup(r => r.GetById(request.AlbumId)).Returns(NewAlbum());
        _ratingRepository.Setup(r => r.Exists(request.UserId, request.AlbumId)).Returns(true);

        // Act
        var act = () => _service.Create(request);

        // Assert
        Assert.Throws<ConflictException>(act);
        _ratingRepository.Verify(r => r.Create(It.IsAny<AlbumRating>()), Times.Never);
    }

    [Fact]
    public void Create_DadosValidos_PersisteUmaVezERetornaAvaliacao()
    {
        // Arrange
        var request = new AlbumRatingRequest(Guid.NewGuid(), Guid.NewGuid(), 5, "Classico");
        _userRepository.Setup(r => r.GetById(request.UserId)).Returns(NewUser());
        _albumRepository.Setup(r => r.GetById(request.AlbumId)).Returns(NewAlbum());
        _ratingRepository.Setup(r => r.Exists(request.UserId, request.AlbumId)).Returns(false);
        _ratingRepository
            .Setup(r => r.Create(It.IsAny<AlbumRating>()))
            .Returns((AlbumRating rating) => rating);

        // Act
        var response = _service.Create(request);

        // Assert
        Assert.Equal(request.UserId, response.UserId);
        Assert.Equal(request.AlbumId, response.AlbumId);
        Assert.Equal(5, response.Score);
        _ratingRepository.Verify(
            r => r.Create(It.Is<AlbumRating>(a => a.UserId == request.UserId && a.AlbumId == request.AlbumId)),
            Times.Once);
    }
}

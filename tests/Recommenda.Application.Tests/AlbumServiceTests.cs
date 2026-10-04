using Moq;
using Recommenda.Application.Common;
using Recommenda.Application.DTOs;
using Recommenda.Application.Services;
using Recommenda.Domain.Entities;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Application.Tests;

/// <summary>
/// Testes do AlbumService (criacao e paginacao) com mocks dos repositorios.
/// </summary>
public class AlbumServiceTests
{
    private readonly Mock<IAlbumRepository>  _albumRepository  = new();
    private readonly Mock<IArtistRepository> _artistRepository = new();
    private readonly AlbumService            _service;

    public AlbumServiceTests()
    {
        _service = new AlbumService(_albumRepository.Object, _artistRepository.Object);
    }

    private static List<Album> NewAlbums(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new Album($"Album {i}", new DateTime(2000 + i, 1, 1), Guid.NewGuid()))
            .ToList();

    // ---------------------------------------------------------------------
    // Criacao
    // ---------------------------------------------------------------------

    [Fact]
    public void Create_ArtistaInexistente_LancaResourceNotFoundExceptionENaoPersiste()
    {
        // Arrange
        var request = new AlbumRequest("Nada", new DateTime(2020, 1, 1), Guid.NewGuid());
        _artistRepository.Setup(r => r.GetById(request.ArtistId)).Returns((Artist?)null);

        // Act
        var act = () => _service.Create(request);

        // Assert
        Assert.Throws<ResourceNotFoundException>(act);
        _albumRepository.Verify(r => r.Create(It.IsAny<Album>()), Times.Never);
    }

    [Fact]
    public void Create_ArtistaExistente_PersisteUmaVez()
    {
        // Arrange
        var artist  = new Artist("Criolo", "Rapper brasileiro.", "Brasil");
        var request = new AlbumRequest("No na Orelha", new DateTime(2011, 7, 12), artist.Id);
        _artistRepository.Setup(r => r.GetById(request.ArtistId)).Returns(artist);
        _albumRepository.Setup(r => r.Create(It.IsAny<Album>())).Returns((Album album) => album);

        // Act
        var response = _service.Create(request);

        // Assert
        Assert.Equal("No na Orelha", response.Title);
        Assert.Equal(artist.Id, response.ArtistId);
        _albumRepository.Verify(r => r.Create(It.IsAny<Album>()), Times.Once);
    }

    // ---------------------------------------------------------------------
    // Paginacao
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    [InlineData(1, 101)]
    [InlineData(1, 9999)]
    public void GetPaged_ParametrosForaDaFaixa_LancaInvalidPageRequestExceptionESemConsultarRepositorio(
        int page, int pageSize)
    {
        // Arrange — nada a configurar: a validacao vem antes do repositorio

        // Act
        var act = () => _service.GetPaged(page, pageSize);

        // Assert
        Assert.Throws<InvalidPageRequestException>(act);
        _albumRepository.Verify(r => r.GetPaged(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 20)]
    [InlineData(3, 100)]
    public void GetPaged_ParametrosNaFaixa_RepassaPaginaAoRepositorio(int page, int pageSize)
    {
        // Arrange
        _albumRepository
            .Setup(r => r.GetPaged(page, pageSize))
            .Returns(new PagedResult<Album>([], 0));

        // Act
        var response = _service.GetPaged(page, pageSize);

        // Assert
        Assert.Equal(page, response.Page);
        Assert.Equal(pageSize, response.PageSize);
        _albumRepository.Verify(r => r.GetPaged(page, pageSize), Times.Once);
    }

    [Fact]
    public void GetPaged_PrimeiraPagina_RetornaEnvelopeComTotaisCoerentes()
    {
        // Arrange — 5 albuns no total, pagina 1 com 2 itens
        var pageItems = NewAlbums(2);
        _albumRepository
            .Setup(r => r.GetPaged(1, 2))
            .Returns(new PagedResult<Album>(pageItems, 5));

        // Act
        var response = _service.GetPaged(1, 2);

        // Assert
        Assert.Equal(1, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(5, response.TotalItems);
        Assert.Equal(3, response.TotalPages); // teto(5 / 2)
        Assert.False(response.HasPrevious);
        Assert.True(response.HasNext);
        Assert.Equal(2, response.Items.Count);
    }

    [Fact]
    public void GetPaged_PaginaAlemDoTotal_RetornaItensVaziosSemErro()
    {
        // Arrange
        _albumRepository
            .Setup(r => r.GetPaged(50, 2))
            .Returns(new PagedResult<Album>([], 5));

        // Act
        var response = _service.GetPaged(50, 2);

        // Assert
        Assert.Empty(response.Items);
        Assert.Equal(5, response.TotalItems);
        Assert.Equal(3, response.TotalPages);
        Assert.False(response.HasNext);
    }
}

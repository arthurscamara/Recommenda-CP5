using Recommenda.Domain.Entities;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Tests;

/// <summary>
/// Regras do album: titulo obrigatorio e data de lancamento a partir de 1877
/// (ano do fonografo — nao existe gravacao anterior).
/// </summary>
public class AlbumTests
{
    [Fact]
    public void Constructor_DadosValidos_CriaAlbum()
    {
        // Arrange
        var artistId    = Guid.NewGuid();
        var releaseDate = new DateTime(2011, 7, 12);

        // Act
        var album = new Album("No na Orelha", releaseDate, artistId, "https://example.com/capa.jpg");

        // Assert
        Assert.Equal("No na Orelha", album.Title);
        Assert.Equal(releaseDate, album.ReleaseDate);
        Assert.Equal(artistId, album.ArtistId);
        Assert.Equal("https://example.com/capa.jpg", album.CoverUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Constructor_TituloVazio_LancaDomainException(string? invalidTitle)
    {
        // Arrange
        var releaseDate = new DateTime(2020, 1, 1);

        // Act
        var act = () => new Album(invalidTitle!, releaseDate, Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal("Título do álbum não pode ser vazio.", exception.Message);
    }

    [Theory]
    [InlineData(1876)]
    [InlineData(1500)]
    [InlineData(1)]
    public void Constructor_DataAnteriorA1877_LancaDomainException(int year)
    {
        // Arrange
        var releaseDate = new DateTime(year, 1, 1);

        // Act
        var act = () => new Album("Album Antigo", releaseDate, Guid.NewGuid());

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Constructor_DataExatamenteEm1877_CriaAlbum()
    {
        // Arrange
        var releaseDate = new DateTime(1877, 12, 6);

        // Act
        var album = new Album("Mary Had a Little Lamb", releaseDate, Guid.NewGuid());

        // Assert
        Assert.Equal(1877, album.ReleaseDate.Year);
    }
}

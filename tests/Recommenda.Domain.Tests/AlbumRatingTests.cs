using Recommenda.Domain.Entities;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Tests;

/// <summary>
/// Regra do MER: a nota de uma avaliacao de album fica na faixa 1..5.
/// </summary>
public class AlbumRatingTests
{
    [Fact]
    public void Constructor_NotaDentroDaFaixa_CriaAvaliacaoComDadosInformados()
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var albumId = Guid.NewGuid();
        const int score = 4;
        const string comment = "Disco muito bom.";

        // Act
        var rating = new AlbumRating(userId, albumId, score, comment);

        // Assert
        Assert.Equal(userId, rating.UserId);
        Assert.Equal(albumId, rating.AlbumId);
        Assert.Equal(score, rating.Score);
        Assert.Equal(comment, rating.Comment);
        Assert.NotEqual(Guid.Empty, rating.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Constructor_NotaForaDaFaixa_LancaDomainException(int invalidScore)
    {
        // Arrange
        var userId  = Guid.NewGuid();
        var albumId = Guid.NewGuid();

        // Act
        var exception = Assert.Throws<DomainException>(() => new AlbumRating(userId, albumId, invalidScore));

        // Assert
        Assert.Equal("Nota deve estar entre 1 e 5.", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void UpdateScore_NotaNosLimitesDaFaixa_AtualizaNota(int newScore)
    {
        // Arrange
        var rating = new AlbumRating(Guid.NewGuid(), Guid.NewGuid(), 2);

        // Act
        rating.UpdateScore(newScore);

        // Assert
        Assert.Equal(newScore, rating.Score);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void UpdateScore_NotaForaDaFaixa_LancaDomainExceptionEMantemNotaAnterior(int invalidScore)
    {
        // Arrange
        var rating = new AlbumRating(Guid.NewGuid(), Guid.NewGuid(), 3);

        // Act
        var act = () => rating.UpdateScore(invalidScore);

        // Assert
        Assert.Throws<DomainException>(act);
        Assert.Equal(3, rating.Score);
    }
}

using Recommenda.Domain.Entities;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Tests;

/// <summary>
/// Regras da faixa: duracao e numero da faixa positivos; duracao formatada mm:ss.
/// </summary>
public class TrackTests
{
    [Theory]
    [InlineData(214, "3:34")]
    [InlineData(60, "1:00")]
    [InlineData(5, "0:05")]
    [InlineData(3600, "60:00")]
    public void FormattedDuration_DuracaoEmSegundos_RetornaMinutosESegundos(int seconds, string expected)
    {
        // Arrange
        var track = new Track("Subirusdoistiozin", seconds, 1, Guid.NewGuid());

        // Act
        var formatted = track.FormattedDuration;

        // Assert
        Assert.Equal(expected, formatted);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-10, 1)]
    [InlineData(200, 0)]
    [InlineData(200, -3)]
    public void Constructor_DuracaoOuNumeroInvalido_LancaDomainException(int durationSeconds, int trackNumber)
    {
        // Arrange
        var albumId = Guid.NewGuid();

        // Act
        var act = () => new Track("Faixa", durationSeconds, trackNumber, albumId);

        // Assert
        Assert.Throws<DomainException>(act);
    }
}

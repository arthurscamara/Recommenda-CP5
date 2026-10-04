using Recommenda.Domain.Common;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Entities;

/// <summary>
/// Faixa musical dentro de um álbum.
/// </summary>
public class Track : BaseEntity
{
    public string Title { get; private set; } = string.Empty;

    /// <summary>Duração em segundos.</summary>
    public int DurationSeconds { get; private set; }

    public int TrackNumber { get; private set; }

    // FK obrigatória
    public Guid AlbumId { get; private set; }

    public Album? Album { get; private set; }

    // N:N — uma faixa pode ter vários gêneros
    public List<Genre> Genres { get; private set; } = [];

    // 1:N — uma faixa pode ter várias avaliações
    public List<TrackRating> Ratings { get; private set; } = [];

    private Track() { }

    public Track(string title, int durationSeconds, int trackNumber, Guid albumId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Título da faixa não pode ser vazio.");
        if (durationSeconds <= 0)
            throw new DomainException("Duração deve ser maior que zero.");
        if (trackNumber < 1)
            throw new DomainException("Número da faixa deve ser maior que zero.");

        Title = title;
        DurationSeconds = durationSeconds;
        TrackNumber = trackNumber;
        AlbumId = albumId;
    }

    public string FormattedDuration =>
        $"{DurationSeconds / 60}:{DurationSeconds % 60:D2}";
}

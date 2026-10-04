using Recommenda.Domain.Common;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Entities;

/// <summary>
/// Álbum de estúdio, EP ou coletânea de um artista.
/// </summary>
public class Album : BaseEntity
{
    public string Title { get; private set; } = string.Empty;

    public DateTime ReleaseDate { get; private set; }

    public string CoverUrl { get; private set; } = string.Empty;

    // FK obrigatória — todo álbum pertence a um artista
    public Guid ArtistId { get; private set; }

    public Artist? Artist { get; private set; }

    // 1:N — um álbum tem várias faixas
    public List<Track> Tracks { get; private set; } = [];

    // 1:N — um álbum pode ter várias avaliações
    public List<AlbumRating> Ratings { get; private set; } = [];

    private Album() { }

    public Album(string title, DateTime releaseDate, Guid artistId, string coverUrl = "")
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Título do álbum não pode ser vazio.");
        if (releaseDate.Year < 1877)
            throw new DomainException("Data de lançamento inválida.");

        Title = title;
        ReleaseDate = releaseDate;
        ArtistId = artistId;
        CoverUrl = coverUrl;
    }
}

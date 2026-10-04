using Recommenda.Domain.Common;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Entities;

/// <summary>
/// Avaliação de um usuário sobre um álbum (1–5).
/// </summary>
public class AlbumRating : BaseEntity
{
    public Guid UserId { get; private set; }

    public Guid AlbumId { get; private set; }

    public int Score { get; private set; }

    public string? Comment { get; private set; }

    private AlbumRating() { }

    public AlbumRating(Guid userId, Guid albumId, int score, string? comment = null)
    {
        UserId = userId;
        AlbumId = albumId;
        UpdateScore(score);
        Comment = comment;
    }

    public void UpdateScore(int score)
    {
        if (score is < 1 or > 5)
            throw new DomainException("Nota deve estar entre 1 e 5.");
        Score = score;
    }
}

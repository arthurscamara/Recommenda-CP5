using Recommenda.Domain.Common;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Entities;

/// <summary>
/// Avaliação de um usuário sobre uma faixa (1–5).
/// </summary>
public class TrackRating : BaseEntity
{
    public Guid UserId { get; private set; }

    public Guid TrackId { get; private set; }

    public int Score { get; private set; }

    public string? Comment { get; private set; }

    private TrackRating() { }

    public TrackRating(Guid userId, Guid trackId, int score, string? comment = null)
    {
        UserId = userId;
        TrackId = trackId;
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

using Recommenda.Domain.Common;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Entities;

/// <summary>
/// Gênero musical (Rock, Pop, Jazz, etc.).
/// </summary>
public class Genre : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    // N:N
    public List<Artist> Artists { get; private set; } = [];

    // N:N
    public List<Track> Tracks { get; private set; } = [];

    private Genre() { }

    public Genre(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do gênero não pode ser vazio.");
        Name = name;
        Description = description;
    }
}

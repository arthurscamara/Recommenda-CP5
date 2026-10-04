using Recommenda.Domain.Common;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Entities;

/// <summary>
/// Representa um artista ou banda musical.
/// </summary>
public class Artist : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Bio { get; private set; } = string.Empty;

    public string Country { get; private set; } = string.Empty;

    // 1:N — um artista tem vários álbuns
    public List<Album> Albums { get; private set; } = [];

    // N:N — um artista pode ter vários gêneros
    public List<Genre> Genres { get; private set; } = [];

    private Artist() { }

    public Artist(string name, string bio, string country)
    {
        UpdateName(name);
        Bio = bio;
        Country = country;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome do artista não pode ser vazio.");
        Name = name;
    }

    public void UpdateBio(string bio) => Bio = bio;
}

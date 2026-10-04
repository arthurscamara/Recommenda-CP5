using Recommenda.Domain.Common;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Entities;

/// <summary>
/// Playlist criada por um usuário contendo várias faixas.
/// </summary>
public class Playlist : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public bool IsPublic { get; private set; }

    // FK obrigatória
    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    // N:N — uma playlist tem várias faixas
    public List<Track> Tracks { get; private set; } = [];

    private Playlist() { }

    public Playlist(string name, Guid userId, string description = "", bool isPublic = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome da playlist não pode ser vazio.");
        Name = name;
        UserId = userId;
        Description = description;
        IsPublic = isPublic;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new DomainException("Nome da playlist não pode ser vazio.");
        Name = newName;
    }
}

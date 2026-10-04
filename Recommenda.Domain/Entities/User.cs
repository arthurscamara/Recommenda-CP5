using Recommenda.Domain.Common;
using Recommenda.Domain.Exceptions;

namespace Recommenda.Domain.Entities;

/// <summary>
/// Usuário da plataforma de descoberta musical.
/// </summary>
public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string Password { get; private set; } = string.Empty;

    private string Salt { get; set; } = string.Empty;

    private DateOnly BirthDate { get; set; }

    // 1:N
    public List<AlbumRating> AlbumRatings { get; private set; } = [];

    // 1:N
    public List<TrackRating> TrackRatings { get; private set; } = [];

    // 1:N — um usuário pode ter várias playlists
    public List<Playlist> Playlists { get; private set; } = [];

    // 1:1 opcional
    public UserProfile? Profile { get; private set; }

    private User() { }

    public User(string name, string email, DateOnly birthDate, string rawPassword)
    {
        UpdateName(name);
        UpdateEmail(email);
        SetBirthDate(birthDate);
        ChangePassword(rawPassword);
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Nome não pode ser vazio.");
        Name = name;
    }

    public void UpdateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("E-mail inválido.");
        Email = email;
    }

    public void SetBirthDate(DateOnly date)
    {
        var age = CalculateAge(date);
        if (age < 13) throw new DomainException("Usuário deve ter pelo menos 13 anos.");
        BirthDate = date;
    }

    public void ChangePassword(string rawPassword)
    {
        if (string.IsNullOrWhiteSpace(rawPassword) || rawPassword.Length < 8)
            throw new DomainException("Senha deve ter pelo menos 8 caracteres.");

        Salt = Guid.NewGuid().ToString("N");
        Password = BCrypt.Net.BCrypt.HashPassword(rawPassword + Salt);
    }

    public int Age => CalculateAge(BirthDate);

    private static int CalculateAge(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = today.Year - date.Year;
        if (date > today.AddYears(-age)) age--;
        return age;
    }
}

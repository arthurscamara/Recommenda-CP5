using Microsoft.EntityFrameworkCore;
using Recommenda.Application.Common;
using Recommenda.Application.Services;
using Recommenda.Domain.Entities;
using Recommenda.Infrastructure.Persistence;

namespace Recommenda.Infrastructure;

public sealed class AlbumRepository(RecommendaContext context) : IAlbumRepository
{
    public IReadOnlyList<Album> GetAll() =>
        context.Albums.Include(a => a.Artist).OrderBy(a => a.Title).ToList();

    /// <summary>
    /// Pagina cortada no banco: COUNT + ORDER BY + LIMIT/OFFSET.
    /// So materializa (ToList) depois do Skip/Take.
    /// </summary>
    public PagedResult<Album> GetPaged(int page, int pageSize)
    {
        var query = context.Albums.AsNoTracking();

        var totalItems = query.Count();

        // long evita overflow com page muito grande; alem do total -> pagina vazia
        var offset = (long)(page - 1) * pageSize;
        if (offset >= totalItems)
            return new PagedResult<Album>([], totalItems);

        var items = query
            .OrderBy(a => a.Title)
            .ThenBy(a => a.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .ToList();

        return new PagedResult<Album>(items, totalItems);
    }

    public Album? GetById(Guid id) =>
        context.Albums.Include(a => a.Artist).Include(a => a.Tracks).FirstOrDefault(a => a.Id == id);

    public IReadOnlyList<Album> GetByArtist(Guid artistId) =>
        context.Albums.Where(a => a.ArtistId == artistId).OrderBy(a => a.ReleaseDate).ToList();

    public Album Create(Album album)
    {
        context.Albums.Add(album);
        context.SaveChanges();
        return album;
    }

    public bool Delete(Guid id)
    {
        var album = context.Albums.FirstOrDefault(a => a.Id == id);
        if (album is null) return false;
        context.Albums.Remove(album);
        context.SaveChanges();
        return true;
    }
}

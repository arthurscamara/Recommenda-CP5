using Microsoft.EntityFrameworkCore;
using Recommenda.Application.Common;
using Recommenda.Application.Repositories;
using Recommenda.Domain.Common;
using Recommenda.Infrastructure.Persistence;

namespace Recommenda.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementacao generica de <see cref="IRepository{T}"/> usando Entity Framework Core.
/// Cobre operacoes CRUD basicas para qualquer entidade que herde de <see cref="BaseEntity"/>.
/// </summary>
/// <typeparam name="T">Tipo da entidade de dominio.</typeparam>
public class Repository<T>(RecommendaContext context) : IRepository<T> where T : BaseEntity
{
    protected readonly RecommendaContext Context = context;
    private readonly DbSet<T> _set = context.Set<T>();

    /// <inheritdoc/>
    public IReadOnlyList<T> GetAll()
    {
        return _set
            .AsNoTracking()
            .OrderBy(e => e.CreatedAt)
            .ToList();
    }

    /// <inheritdoc/>
    public PagedResult<T> GetPaged(int page, int pageSize)
    {
        var query = _set.AsNoTracking();

        var totalItems = query.Count();

        var offset = (long)(page - 1) * pageSize;
        if (offset >= totalItems)
            return new PagedResult<T>([], totalItems);

        var items = query
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>(items, totalItems);
    }

    /// <inheritdoc/>
    public T? GetById(Guid id)
    {
        return _set.Find(id);
    }

    /// <inheritdoc/>
    public T Add(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _set.Add(entity);
        Context.SaveChanges();
        return entity;
    }

    /// <inheritdoc/>
    public bool Delete(Guid id)
    {
        var entity = GetById(id);
        if (entity is null) return false;
        _set.Remove(entity);
        Context.SaveChanges();
        return true;
    }

    /// <inheritdoc/>
    public bool ExistsById(Guid id)
    {
        return _set.AsNoTracking().Any(e => e.Id == id);
    }
}

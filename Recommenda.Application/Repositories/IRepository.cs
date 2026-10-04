using Recommenda.Application.Common;
using Recommenda.Domain.Common;

namespace Recommenda.Application.Repositories;

/// <summary>
/// Contrato generico de persistencia para entidades que derivam de <see cref="BaseEntity"/>.
/// Fornece operacoes CRUD minimas sem depender de qualquer tecnologia de acesso a dados.
/// </summary>
/// <typeparam name="T">Tipo da entidade de dominio. Deve herdar de <see cref="BaseEntity"/>.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>Retorna todas as entidades ativas.</summary>
    IReadOnlyList<T> GetAll();

    /// <summary>
    /// Retorna uma pagina de entidades. O corte (Count + OrderBy + Skip + Take)
    /// e feito no banco, nunca em memoria.
    /// </summary>
    /// <param name="page">Pagina (base 1), ja validada.</param>
    /// <param name="pageSize">Tamanho da pagina, ja validado.</param>
    PagedResult<T> GetPaged(int page, int pageSize);

    /// <summary>Busca uma entidade pelo identificador unico.</summary>
    /// <param name="id">Identificador da entidade.</param>
    T? GetById(Guid id);

    /// <summary>Persiste uma nova entidade e retorna o objeto salvo.</summary>
    /// <param name="entity">Entidade a ser adicionada.</param>
    T Add(T entity);

    /// <summary>Remove uma entidade pelo identificador.</summary>
    /// <param name="id">Identificador da entidade.</param>
    /// <returns><c>true</c> se removida; <c>false</c> se nao encontrada.</returns>
    bool Delete(Guid id);

    /// <summary>Verifica se existe uma entidade com o identificador informado.</summary>
    /// <param name="id">Identificador a verificar.</param>
    bool ExistsById(Guid id);
}

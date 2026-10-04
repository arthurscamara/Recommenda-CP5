namespace Recommenda.Application.DTOs;

/// <summary>
/// Envelope paginado devolvido pela listagem v2.
/// </summary>
/// <typeparam name="T">Tipo do item.</typeparam>
/// <param name="Page">Pagina atual (base 1).</param>
/// <param name="PageSize">Tamanho da pagina.</param>
/// <param name="TotalItems">Total de registros existentes.</param>
/// <param name="TotalPages">Total de paginas (teto de totalItems / pageSize).</param>
/// <param name="HasPrevious">Existe pagina anterior.</param>
/// <param name="HasNext">Existe proxima pagina.</param>
/// <param name="Items">Itens da pagina.</param>
public sealed record PagedResponse<T>(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    bool HasPrevious,
    bool HasNext,
    IReadOnlyList<T> Items)
{
    public static PagedResponse<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)pageSize);

        return new PagedResponse<T>(
            page,
            pageSize,
            totalItems,
            totalPages,
            HasPrevious: page > 1,
            HasNext: page < totalPages,
            items);
    }
}

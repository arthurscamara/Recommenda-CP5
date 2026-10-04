namespace Recommenda.Application.Common;

/// <summary>
/// Resultado bruto de uma consulta paginada vinda do repositorio:
/// os itens da pagina pedida e o total de registros (antes do corte).
/// </summary>
/// <typeparam name="T">Tipo do item.</typeparam>
/// <param name="Items">Itens da pagina (ja cortados no banco).</param>
/// <param name="TotalItems">Total de registros existentes.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalItems);

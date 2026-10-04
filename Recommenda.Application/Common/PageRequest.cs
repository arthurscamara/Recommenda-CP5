namespace Recommenda.Application.Common;

/// <summary>
/// Parametros de paginacao validados.
/// Regras: page &gt;= 1 e pageSize entre 1 e <see cref="MaxPageSize"/>.
/// </summary>
public sealed record PageRequest
{
    public const int DefaultPage     = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize     = 100;

    public int Page { get; }
    public int PageSize { get; }

    private PageRequest(int page, int pageSize)
    {
        Page     = page;
        PageSize = pageSize;
    }

    /// <summary>
    /// Cria um <see cref="PageRequest"/> validando o intervalo.
    /// </summary>
    /// <exception cref="InvalidPageRequestException">Quando page ou pageSize estao fora da faixa.</exception>
    public static PageRequest Create(int page, int pageSize)
    {
        if (page < 1)
            throw new InvalidPageRequestException(
                $"O parametro 'page' deve ser maior ou igual a 1 (recebido: {page}).");

        if (pageSize is < 1 or > MaxPageSize)
            throw new InvalidPageRequestException(
                $"O parametro 'pageSize' deve estar entre 1 e {MaxPageSize} (recebido: {pageSize}).");

        return new PageRequest(page, pageSize);
    }
}

/// <summary>
/// Lancada quando os parametros de paginacao estao fora da faixa permitida.
/// Herda de <see cref="ArgumentException"/> e e mapeada para HTTP 400.
/// </summary>
public sealed class InvalidPageRequestException(string message) : ArgumentException(message);

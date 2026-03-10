namespace ApiGrupos.Models;

public class PaginacaoQuery
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 150000;

    public int? Page { get; set; }
    public int? PageSize { get; set; }

    public bool HasPagination => Page.HasValue || PageSize.HasValue;

    public bool TryResolve(out int page, out int pageSize, out string? error)
    {
        page = Page ?? DefaultPage;
        pageSize = PageSize ?? DefaultPageSize;
        error = null;

        if (page < 1)
        {
            error = "O parametro 'page' deve ser maior ou igual a 1.";
            return false;
        }

        if (pageSize < 1)
        {
            error = "O parametro 'pageSize' deve ser maior ou igual a 1.";
            return false;
        }

        if (pageSize > MaxPageSize)
        {
            pageSize = MaxPageSize;
        }

        return true;
    }
}

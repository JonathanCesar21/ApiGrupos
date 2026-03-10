namespace ApiGrupos.Models;

public class PaginacaoResposta<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<T> Items { get; set; } = Array.Empty<T>();
}

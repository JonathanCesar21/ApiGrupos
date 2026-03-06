namespace ApiGrupos.Models;

public class Subgrupo
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int? GrupoCodigo { get; set; }
}

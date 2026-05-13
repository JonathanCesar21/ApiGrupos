using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class Categoria
{
    [JsonPropertyName("CodCategoria")]
    public int CodCategoria { get; set; }

    [JsonPropertyName("NomeCategoria")]
    public string NomeCategoria { get; set; } = string.Empty;
}

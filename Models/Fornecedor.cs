using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class Fornecedor
{
    [JsonPropertyName("cod_for")]
    public int CodFor { get; set; }

    [JsonPropertyName("nome_for")]
    public string NomeFor { get; set; } = string.Empty;
}

using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class Colecao
{
    [JsonPropertyName("colecao")]
    public string Nome { get; set; } = string.Empty;
}

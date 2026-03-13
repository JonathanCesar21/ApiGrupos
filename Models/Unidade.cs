using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class Unidade
{
    [JsonPropertyName("CodUnidade")]
    public int CodUnidade { get; set; }

    [JsonPropertyName("NomeUnidade")]
    public string NomeUnidade { get; set; } = string.Empty;
}

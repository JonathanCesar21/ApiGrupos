using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class Numeracao
{
    [JsonPropertyName("CodNumero")]
    public int CodNumero { get; set; }

    [JsonPropertyName("Numero")]
    public string Numero { get; set; } = string.Empty;
}

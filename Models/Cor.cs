using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class Cor
{
    [JsonPropertyName("CodCor")]
    public int CodCor { get; set; }

    [JsonPropertyName("NomeCor")]
    public string NomeCor { get; set; } = string.Empty;
}

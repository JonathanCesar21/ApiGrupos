using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class Ncm
{
    [JsonPropertyName("CodNcm")]
    public int CodNcm { get; set; }

    [JsonPropertyName("NomeNcm")]
    public string NomeNcm { get; set; } = string.Empty;
}

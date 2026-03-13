using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class SituacaoTributaria
{
    [JsonPropertyName("CodSituacaoTributaria")]
    public int CodSituacaoTributaria { get; set; }

    [JsonPropertyName("Descricao")]
    public string Descricao { get; set; } = string.Empty;
}

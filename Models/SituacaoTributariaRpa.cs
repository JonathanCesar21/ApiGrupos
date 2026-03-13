using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class SituacaoTributariaRpa
{
    [JsonPropertyName("CodSituacaoTributariaRPA")]
    public int CodSituacaoTributariaRpa { get; set; }

    [JsonPropertyName("SituacaoTributariaRPA")]
    public string NomeSituacaoTributariaRpa { get; set; } = string.Empty;
}

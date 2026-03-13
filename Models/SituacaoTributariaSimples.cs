using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class SituacaoTributariaSimples
{
    [JsonPropertyName("CodSituacaoTributariaSimples")]
    public int CodSituacaoTributariaSimples { get; set; }

    [JsonPropertyName("SituacaoTributariaSimples")]
    public string NomeSituacaoTributariaSimples { get; set; } = string.Empty;
}

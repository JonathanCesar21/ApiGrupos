using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class ProdutoBarra
{
    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("barras")]
    public string Barras { get; set; } = string.Empty;

    [JsonPropertyName("SubGrupo")]
    public string SubGrupo { get; set; } = string.Empty;

    [JsonPropertyName("DescProd")]
    public string DescProd { get; set; } = string.Empty;

    [JsonPropertyName("Grupo")]
    public string Grupo { get; set; } = string.Empty;

    [JsonPropertyName("Numero")]
    public string Numero { get; set; } = string.Empty;

    [JsonPropertyName("Cor")]
    public string Cor { get; set; } = string.Empty;
}

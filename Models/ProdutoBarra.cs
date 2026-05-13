using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class ProdutoBarra
{
    [JsonPropertyName("CodProd")]
    public int? CodProd { get; set; }

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

    [JsonPropertyName("CodSecao")]
    public int? CodSecao { get; set; }

    [JsonPropertyName("CodClassificacao")]
    public int? CodClassificacao { get; set; }

    [JsonPropertyName("CodCategoria")]
    public int? CodCategoria { get; set; }

    [JsonPropertyName("Numero")]
    public string Numero { get; set; } = string.Empty;

    [JsonPropertyName("Cor")]
    public string Cor { get; set; } = string.Empty;

    [JsonPropertyName("Colecao")]
    public string Colecao { get; set; } = string.Empty;

    [JsonPropertyName("NomeFornecedor")]
    public string NomeFornecedor { get; set; } = string.Empty;

    [JsonPropertyName("CodFornecedor")]
    public int? CodFornecedor { get; set; }

    [JsonPropertyName("ValorCusto")]
    public decimal ValorCusto { get; set; }

    [JsonPropertyName("Valor")]
    public decimal Valor { get; set; }
}

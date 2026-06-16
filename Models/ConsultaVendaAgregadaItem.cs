using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class ConsultaVendaAgregadaItem
{
    [JsonPropertyName("data_venda")]
    public DateOnly DataVenda { get; set; }

    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("numero")]
    public string Numero { get; set; } = string.Empty;

    [JsonPropertyName("loja")]
    public string Loja { get; set; } = string.Empty;

    [JsonPropertyName("grupo")]
    public string Grupo { get; set; } = string.Empty;

    [JsonPropertyName("subgrupo")]
    public string SubGrupo { get; set; } = string.Empty;

    [JsonPropertyName("fornecedor")]
    public string Fornecedor { get; set; } = string.Empty;

    [JsonPropertyName("qtde_venda")]
    public decimal QtdeVenda { get; set; }

    [JsonPropertyName("total_venda")]
    public decimal TotalVenda { get; set; }

    [JsonPropertyName("total_custo")]
    public decimal TotalCusto { get; set; }
}

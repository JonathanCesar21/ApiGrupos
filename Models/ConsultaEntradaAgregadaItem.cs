using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class ConsultaEntradaAgregadaItem
{
    [JsonPropertyName("data_entrada")]
    public DateOnly DataEntrada { get; set; }

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

    [JsonPropertyName("qtde_entrada")]
    public decimal QtdeEntrada { get; set; }

    [JsonPropertyName("valor_entrada")]
    public decimal ValorEntrada { get; set; }
}

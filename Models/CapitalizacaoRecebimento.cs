using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class CapitalizacaoRecebimento
{
    [JsonPropertyName("codFormaPagamento")]
    public int? CodFormaPagamento { get; set; }

    [JsonPropertyName("formaPagamento")]
    public string? FormaPagamento { get; set; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }

    [JsonPropertyName("operador")]
    public string? Operador { get; set; }

    [JsonPropertyName("valor")]
    public decimal? Valor { get; set; }

    [JsonPropertyName("vlpago")]
    public decimal? Vlpago { get; set; }

    [JsonPropertyName("baixa")]
    public DateTime? Baixa { get; set; }

    [JsonPropertyName("loja")]
    public int? Loja { get; set; }

    [JsonPropertyName("lojaRecebimento")]
    public int? LojaRecebimento { get; set; }

    [JsonPropertyName("vencimento")]
    public DateTime? Vencimento { get; set; }

    [JsonPropertyName("pedido")]
    public string? Pedido { get; set; }
}

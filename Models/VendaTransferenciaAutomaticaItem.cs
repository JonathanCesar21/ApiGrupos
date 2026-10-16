using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class VendaTransferenciaAutomaticaItem
{
    [JsonPropertyName("referencia")]
    public string? Referencia { get; set; }

    [JsonPropertyName("Cor")]
    public string? Cor { get; set; }

    [JsonPropertyName("Numero")]
    public string? Numero { get; set; }

    [JsonPropertyName("barras")]
    public string? Barras { get; set; }

    [JsonPropertyName("quant")]
    public decimal? Quant { get; set; }

    [JsonPropertyName("TransferenciaAutomatica")]
    public int TransferenciaAutomatica { get; set; }

    [JsonPropertyName("LojaOrigemEstoque")]
    public string? LojaOrigemEstoque => TransferenciaAutomatica == 1 ? "05" : null;
}

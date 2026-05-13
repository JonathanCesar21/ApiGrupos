using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class TransferenciaAutomatica
{
    [JsonPropertyName("referencia")]
    public string Referencia { get; set; } = string.Empty;

    [JsonPropertyName("CodSecao")]
    public int? CodSecao { get; set; }

    [JsonPropertyName("DescSecao")]
    public string DescSecao { get; set; } = string.Empty;

    [JsonPropertyName("codcor")]
    public int? CodCor { get; set; }

    [JsonPropertyName("DescCor")]
    public string DescCor { get; set; } = string.Empty;

    [JsonPropertyName("CodNumero")]
    public int? CodNumero { get; set; }

    [JsonPropertyName("DescNumero")]
    public string DescNumero { get; set; } = string.Empty;

    [JsonPropertyName("Quant")]
    public int Quant { get; set; }

    [JsonPropertyName("NotaFiscal")]
    public string NotaFiscal { get; set; } = string.Empty;

    [JsonPropertyName("Loja")]
    public string Loja { get; set; } = string.Empty;
}

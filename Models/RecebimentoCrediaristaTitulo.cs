using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class RecebimentoCrediaristaTitulo
{
    [JsonPropertyName("dtVencimento")]
    public DateTime DtVencimento { get; set; }

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    [JsonPropertyName("codCli")]
    public int CodCli { get; set; }

    [JsonPropertyName("nomeCliente")]
    public string NomeCliente { get; set; } = string.Empty;

    [JsonPropertyName("bairro")]
    public string? Bairro { get; set; }

    [JsonPropertyName("nomeCidade")]
    public string? NomeCidade { get; set; }

    [JsonPropertyName("dtNascimento")]
    public DateTime? DtNascimento { get; set; }

    [JsonPropertyName("sexo")]
    public string Sexo { get; set; } = string.Empty;

    [JsonPropertyName("codGrupo")]
    public int? CodGrupo { get; set; }

    [JsonPropertyName("limite")]
    public decimal? Limite { get; set; }

    [JsonPropertyName("renda")]
    public decimal? Renda { get; set; }

    [JsonPropertyName("idade")]
    public int? Idade { get; set; }

    [JsonPropertyName("lojaCadastro")]
    public int? LojaCadastro { get; set; }

    [JsonPropertyName("fone")]
    public string? Fone { get; set; }

    [JsonPropertyName("foneReferencia1")]
    public string? FoneReferencia1 { get; set; }

    [JsonPropertyName("foneReferencia2")]
    public string? FoneReferencia2 { get; set; }

    [JsonPropertyName("pedido")]
    public string? Pedido { get; set; }

    [JsonPropertyName("dtPedido")]
    public DateTime? DtPedido { get; set; }

    [JsonPropertyName("parcela")]
    public int? Parcela { get; set; }

    [JsonPropertyName("nParcelas")]
    public int? NParcelas { get; set; }

    [JsonPropertyName("loja")]
    public int? Loja { get; set; }

    [JsonPropertyName("pago")]
    public DateTime? Pago { get; set; }

    [JsonPropertyName("dtBaixa")]
    public DateTime? DtBaixa { get; set; }

    [JsonPropertyName("codFormaPgt")]
    public int? CodFormaPgt { get; set; }

    [JsonPropertyName("formaPagamento")]
    public string? FormaPagamento { get; set; }
}

using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class RecebimentoCrediaristaClienteResumo
{
    [JsonPropertyName("codCli")]
    public int CodCli { get; set; }

    [JsonPropertyName("nomeCliente")]
    public string NomeCliente { get; set; } = string.Empty;

    [JsonPropertyName("bairro")]
    public string? Bairro { get; set; }

    [JsonPropertyName("nomeCidade")]
    public string? NomeCidade { get; set; }

    [JsonPropertyName("limite")]
    public decimal? Limite { get; set; }

    [JsonPropertyName("renda")]
    public decimal? Renda { get; set; }

    [JsonPropertyName("lojaCadastro")]
    public int? LojaCadastro { get; set; }

    [JsonPropertyName("fone")]
    public string? Fone { get; set; }

    [JsonPropertyName("foneReferencia1")]
    public string? FoneReferencia1 { get; set; }

    [JsonPropertyName("foneReferencia2")]
    public string? FoneReferencia2 { get; set; }

    [JsonPropertyName("qtdeTitulos")]
    public int QtdeTitulos { get; set; }

    [JsonPropertyName("valorTotal")]
    public decimal ValorTotal { get; set; }

    [JsonPropertyName("limiteDisponivel")]
    public decimal? LimiteDisponivel { get; set; }

    [JsonPropertyName("primeiroVencimento")]
    public DateTime PrimeiroVencimento { get; set; }

    [JsonPropertyName("ultimoVencimento")]
    public DateTime UltimoVencimento { get; set; }

    [JsonPropertyName("ultimaCompra")]
    public DateTime? UltimaCompra { get; set; }

    [JsonPropertyName("lojas")]
    public string Lojas { get; set; } = string.Empty;
}

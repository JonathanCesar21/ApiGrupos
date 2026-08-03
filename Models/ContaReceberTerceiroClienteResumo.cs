using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class ContaReceberTerceiroClienteResumo
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

    [JsonPropertyName("cpf")]
    public string? Cpf { get; set; }

    [JsonPropertyName("codCobranca")]
    public int? CodCobranca { get; set; }

    [JsonPropertyName("lojaPrincipal")]
    public int? LojaPrincipal { get; set; }

    [JsonPropertyName("qtdeLojas")]
    public int QtdeLojas { get; set; }

    [JsonPropertyName("qtdeTitulos")]
    public int QtdeTitulos { get; set; }

    [JsonPropertyName("qtdePedidos")]
    public int QtdePedidos { get; set; }

    [JsonPropertyName("valorTotalSemJuros")]
    public decimal ValorTotalSemJuros { get; set; }

    [JsonPropertyName("limiteDisponivel")]
    public decimal? LimiteDisponivel { get; set; }

    [JsonPropertyName("primeiroVencimento")]
    public DateTime? PrimeiroVencimento { get; set; }

    [JsonPropertyName("ultimoVencimento")]
    public DateTime? UltimoVencimento { get; set; }

    [JsonPropertyName("primeiroEnvio")]
    public DateTime? PrimeiroEnvio { get; set; }

    [JsonPropertyName("ultimoEnvio")]
    public DateTime? UltimoEnvio { get; set; }

    [JsonPropertyName("ultimaCompra")]
    public DateTime? UltimaCompra { get; set; }

    [JsonPropertyName("proximaDataLimite")]
    public DateTime? ProximaDataLimite { get; set; }

    [JsonPropertyName("ultimoRetorno")]
    public DateTime? UltimoRetorno { get; set; }

    [JsonPropertyName("qtdeComRecebimento")]
    public int QtdeComRecebimento { get; set; }

    [JsonPropertyName("qtdeComRetorno")]
    public int QtdeComRetorno { get; set; }
}

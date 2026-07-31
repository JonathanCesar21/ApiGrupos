using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class Cliente
{
    [JsonPropertyName("codigo")]
    public int Codigo { get; set; }

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

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

    [JsonPropertyName("loja")]
    public int? Loja { get; set; }

    [JsonPropertyName("fone")]
    public string? Fone { get; set; }

    [JsonPropertyName("foneReferencia1")]
    public string? FoneReferencia1 { get; set; }

    [JsonPropertyName("foneReferencia2")]
    public string? FoneReferencia2 { get; set; }
}

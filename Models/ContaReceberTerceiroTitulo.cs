using System.Text.Json.Serialization;

namespace ApiGrupos.Models;

public class ContaReceberTerceiroTitulo
{
    [JsonPropertyName("codFormaPagto")]
    public int? CodFormaPagto { get; set; }

    [JsonPropertyName("formaPagamento")]
    public string? FormaPagamento { get; set; }

    [JsonPropertyName("dtVencimento")]
    public DateTime? DtVencimento { get; set; }

    [JsonPropertyName("valor")]
    public decimal Valor { get; set; }

    [JsonPropertyName("nParcelas")]
    public int? NParcelas { get; set; }

    [JsonPropertyName("pedido")]
    public string? Pedido { get; set; }

    [JsonPropertyName("dtPedido")]
    public DateTime? DtPedido { get; set; }

    [JsonPropertyName("cadastro")]
    public DateTime? Cadastro { get; set; }

    [JsonPropertyName("observacao")]
    public string? Observacao { get; set; }

    [JsonPropertyName("parcela")]
    public int? Parcela { get; set; }

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

    [JsonPropertyName("foneCadastro")]
    public string? FoneCadastro { get; set; }

    [JsonPropertyName("foneReferencia1")]
    public string? FoneReferencia1 { get; set; }

    [JsonPropertyName("foneReferencia2")]
    public string? FoneReferencia2 { get; set; }

    [JsonPropertyName("seq")]
    public long? Seq { get; set; }

    [JsonPropertyName("loteCobranca")]
    public string? LoteCobranca { get; set; }

    [JsonPropertyName("loja")]
    public int? Loja { get; set; }

    [JsonPropertyName("ckSpc")]
    public int? CkSpc { get; set; }

    [JsonPropertyName("sel")]
    public int? Sel { get; set; }

    [JsonPropertyName("codCobranca")]
    public int? CodCobranca { get; set; }

    [JsonPropertyName("dtEnvio")]
    public DateTime? DtEnvio { get; set; }

    [JsonPropertyName("dtRetorno")]
    public DateTime? DtRetorno { get; set; }

    [JsonPropertyName("status")]
    public int? Status { get; set; }

    [JsonPropertyName("cCheque")]
    public int? CCheque { get; set; }

    [JsonPropertyName("emitente")]
    public string? Emitente { get; set; }

    [JsonPropertyName("recebimento")]
    public DateTime? Recebimento { get; set; }

    [JsonPropertyName("banco")]
    public string? Banco { get; set; }

    [JsonPropertyName("cheque")]
    public string? Cheque { get; set; }

    [JsonPropertyName("conta")]
    public string? Conta { get; set; }

    [JsonPropertyName("fone")]
    public string? Fone { get; set; }

    [JsonPropertyName("cpf")]
    public string? Cpf { get; set; }

    [JsonPropertyName("mesAno")]
    public string? MesAno { get; set; }

    [JsonPropertyName("agencia")]
    public string? Agencia { get; set; }

    [JsonPropertyName("dtDevolucao")]
    public DateTime? DtDevolucao { get; set; }

    [JsonPropertyName("dtRetirada")]
    public DateTime? DtRetirada { get; set; }

    [JsonPropertyName("cliente")]
    public int? Cliente { get; set; }

    [JsonPropertyName("cReceber")]
    public long? CReceber { get; set; }

    [JsonPropertyName("seqCheque")]
    public long? SeqCheque { get; set; }

    [JsonPropertyName("carteira")]
    public string? Carteira { get; set; }

    [JsonPropertyName("acordo")]
    public string? Acordo { get; set; }

    [JsonPropertyName("dataLimite")]
    public DateTime? DataLimite { get; set; }

    [JsonPropertyName("operador")]
    public string? Operador { get; set; }

    [JsonPropertyName("codCampanha")]
    public int? CodCampanha { get; set; }

    [JsonPropertyName("nISPC")]
    public string? NIspc { get; set; }

    [JsonPropertyName("nRSPC")]
    public string? NRspc { get; set; }

    [JsonPropertyName("cartorio")]
    public string? Cartorio { get; set; }

    [JsonPropertyName("idEmpresa")]
    public int? IdEmpresa { get; set; }

    [JsonPropertyName("idFilial")]
    public int? IdFilial { get; set; }

    [JsonPropertyName("txAntecipacao")]
    public decimal? TxAntecipacao { get; set; }

    [JsonPropertyName("contratoServipa")]
    public string? ContratoServipa { get; set; }

    [JsonPropertyName("dtCadastro")]
    public DateTime? DtCadastro { get; set; }
}

namespace ApiGrupos.Models;

// Nullable representa NULL da origem; nenhum valor financeiro recebe fallback zero.
public class DashboardRecebimento : CapitalizacaoRecebimento { }

public class DashboardTitulo
{
    public int? CodCli { get; set; }
    public string? Pedido { get; set; }
    public int? Parcela { get; set; }
    public int? NParcelas { get; set; }
    public int? Loja { get; set; }
    public decimal? Valor { get; set; }
    public DateTime? DtVencimento { get; set; }
    public DateTime? DtPedido { get; set; }
    public bool? Pago { get; set; }
    public DateTime? DtBaixa { get; set; }
    public int? CodFormaPgt { get; set; }
    public string? FormaPagamento { get; set; }
}

public class DashboardMetaLoja
{
    public int? Loja { get; set; }
    public int Ano { get; set; }
    public int? Mes { get; set; }
    public DateTime? Data { get; set; }
    public decimal? Vendas { get; set; }
    public decimal? Avista { get; set; }
    public decimal? Receber { get; set; }
    public decimal? Terceiros { get; set; }
    public decimal? ValorRM { get; set; }
    public decimal? ReceberMaisTerceiros => Receber + Terceiros;
}

public class DashboardMetaVendedor
{
    public int? Loja { get; set; }
    public string? CodVend { get; set; }
    public string? Vendedor { get; set; }
    public int Ano { get; set; }
    public int? Mes { get; set; }
    public DateTime? Data { get; set; }
    public decimal? Vendas { get; set; }
}

public class DashboardVendedor
{
    public string? CodVend { get; set; }
    public string? Vendedor { get; set; }
    public int? Loja { get; set; }
}

public class DashboardEntradaConfirmar
{
    public int? Loja { get; set; }
    public decimal? Quant { get; set; }
    public string? Barras { get; set; }
    public string? NotaFiscal { get; set; }
    public DateTime? Data { get; set; }
    public decimal? Valor { get; set; }
    public string? BarrasProduto { get; set; }
    public string? Grupo { get; set; }
    public string? SubGrupo { get; set; }
    public string? Fornecedor { get; set; }
    public string? Referencia { get; set; }
    public string? Secao { get; set; }
    public string? Numero { get; set; }
    public string? Cor { get; set; }
    public decimal? ValorTotal => Quant * Valor;
}

public class DashboardTransferencia
{
    public string? Referencia { get; set; }
    public string? CodSecao { get; set; }
    public string? DescSecao { get; set; }
    public string? CodSubGrupo { get; set; }
    public string? DescSubGrupo { get; set; }
    public string? CodCor { get; set; }
    public string? DescCor { get; set; }
    public string? CodNumero { get; set; }
    public string? DescNumero { get; set; }
    public string? CodFor { get; set; }
    public string? NomeFornecedor { get; set; }
    public decimal? Quant { get; set; }
    public string? NotaFiscal { get; set; }
    public int? Loja { get; set; }
    public int? Origem { get; set; }
    public bool? StatusImpressao { get; set; }
    public DateTime? DataTransferencia { get; set; }
    public DateTime? DataAuto { get; set; }
    public string? UsuarioAuto { get; set; }
    public string? UsuarioConfirma { get; set; }
    public bool? Automatica { get; set; }
    public string? TipoTransferencia { get; set; }
    public string? OrigemRegistro { get; set; }
    public string? Grupo { get; set; }
    public string? Tipo => Automatica switch { true => "Automática", false => "Manual", _ => null };
    public string? Status => StatusImpressao switch
    {
        true => "Preparado Para Envio",
        false => "Não Preparado",
        _ => TipoTransferencia == "MANUAL PENDENTE" ? "Preparado Para Envio" : null
    };
    public string ProdutoCompleto => string.Join(" - ",
        new[] { DescSubGrupo, DescSecao, DescNumero, DescCor }.Where(x => x is not null));
}

public class DashboardAtivacao
{
    public int? CodCli { get; set; }
    public DateTime? DtPrimeiraCompra { get; set; }
    public int? Loja { get; set; }
    public decimal? Valor { get; set; }
    public string? Pedido { get; set; }
    public DateTime? DtCadastro { get; set; }
    public string? Vendedor { get; set; }
    public int Quant => Valor > 0m ? 1 : 0;
}

public class DashboardReferencia
{
    public string? Referencia { get; set; }
    public decimal? Custo { get; set; }
    public string? CodSecao { get; set; }
    public string? Descricao { get; set; }
    public string? CodProd { get; set; }
    public string? Fornecedor { get; set; }
    public string? Grupo { get; set; }
    public string? SubGrupo { get; set; }
    public int? CodCategoria { get; set; }
    public string? Categoria { get; set; }
}

public class DashboardTerceiro
{
    public int? CodCli { get; set; }
    public DateTime? Vencimento { get; set; }
    public decimal? Valor { get; set; }
    public string? Pedido { get; set; }
    public DateTime? DtPedido { get; set; }
    public int? Parcela { get; set; }
    public int? NParcelas { get; set; }
    public int? CodCobranca { get; set; }
    public string? NomeCobranca { get; set; }
    public int? Loja { get; set; }
    public int? Status { get; set; }
}

public class DashboardVendaAgregada
{
    public DateTime? DataVenda { get; set; }
    public string? Referencia { get; set; }
    public string? Numero { get; set; }
    public int? Loja { get; set; }
    public string? Grupo { get; set; }
    public string? SubGrupo { get; set; }
    public string? Fornecedor { get; set; }
    public decimal? Quantidade { get; set; }
    public decimal? FaturamentoPbix { get; set; }
    public decimal? CustoTotalPbix { get; set; }
}

public class DashboardEntradaAgregada
{
    public DateTime? DataEntrada { get; set; }
    public string? Referencia { get; set; }
    public string? Numero { get; set; }
    public int? Loja { get; set; }
    public string? Grupo { get; set; }
    public string? SubGrupo { get; set; }
    public string? Fornecedor { get; set; }
    public decimal? Quantidade { get; set; }
    public decimal? ValorVendaEntradaPbix { get; set; }
    public decimal? CustoTotalEntradaPbix { get; set; }
}

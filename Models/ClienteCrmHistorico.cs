namespace ApiGrupos.Models;

public class ClienteCrmHistorico
{
    public int QtdEncaminhamentosHistorico { get; set; }
    public int QtdAcordosHistorico { get; set; }
    public int QtdAcordosEmAberto { get; set; }
    public int QtdAcordosQuitados { get; set; }
    public int QtdEncaminhamentosTerceirosHistorico { get; set; }
    public int QtdEncaminhamentosTerceirosEmAberto { get; set; }
    public int QtdEncaminhamentosTerceirosQuitados { get; set; }
    public int QtdParcelasPagas12Meses { get; set; }
    public int QtdParcelasPagasComAtraso12Meses { get; set; }
    public int QtdParcelasPagasSemVencimento12Meses { get; set; }
    public decimal? MediaDiasAtraso12Meses { get; set; }
    public decimal? PercentualParcelasPagasComAtraso12Meses { get; set; }
    public int? DiaHabitualPagamento { get; set; }
    public decimal? ConcentracaoDiaHabitualPagamento { get; set; }
    public int QtdDatasPagamento12Meses { get; set; }
    public int QtdMesesComPagamento12Meses { get; set; }
}

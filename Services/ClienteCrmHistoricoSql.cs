namespace ApiGrupos.Services;

public static class ClienteCrmHistoricoSql
{
    // #ClientesCrmPagina e criada pela consulta original, depois da paginacao.
    // As tabelas temporarias pertencem apenas a esta conexao; o ERP nao e alterado.
    public const string Consulta = """
        SET NOCOUNT ON;
        DECLARE @HistoricoReferencia date = CAST(GETDATE() AS date);
        DECLARE @HistoricoInicio date = DATEADD(MONTH, -12, @HistoricoReferencia);
        DECLARE @HistoricoFimExclusivo date = DATEADD(DAY, 1, @HistoricoReferencia);
        CREATE UNIQUE CLUSTERED INDEX IX_ClientesCrmPagina ON #ClientesCrmPagina(Codigo);

        SELECT c.CodCli, CAST(c.Baixa AS date) AS DtPagamento,
               DATEDIFF(DAY, c.Vencimento, c.Baixa) AS DiasAtraso
        INTO #PagamentosCrm
        FROM CReceber c
        INNER JOIN #ClientesCrmPagina p ON p.Codigo = c.CodCli
        WHERE c.Pago IS NOT NULL
          AND c.Baixa >= @HistoricoInicio AND c.Baixa < @HistoricoFimExclusivo
        UNION ALL
        SELECT c.Codcli, CAST(c.Recebimento AS date),
               DATEDIFF(DAY, c.Vencimento, c.Recebimento)
        FROM CReceberCob c
        INNER JOIN #ClientesCrmPagina p ON p.Codigo = c.Codcli
        WHERE c.Recebimento >= @HistoricoInicio AND c.Recebimento < @HistoricoFimExclusivo;

        CREATE CLUSTERED INDEX IX_PagamentosCrm ON #PagamentosCrm(CodCli, DtPagamento);

        SELECT CodCli, COUNT(1) AS QtdPagas,
               SUM(CASE WHEN DiasAtraso > 0 THEN 1 ELSE 0 END) AS QtdAtrasadas,
               SUM(CASE WHEN DiasAtraso IS NULL THEN 1 ELSE 0 END) AS QtdSemVencimento,
               SUM(CONVERT(bigint, CASE WHEN DiasAtraso > 0 THEN DiasAtraso ELSE 0 END)) AS TotalDiasAtraso
        INTO #AtrasosCrm
        FROM #PagamentosCrm
        GROUP BY CodCli;

        SELECT CodCli, DtPagamento,
               DATEADD(MONTH, DATEDIFF(MONTH, 0, DtPagamento), 0) AS MesPagamento,
               CASE WHEN DAY(DATEADD(DAY, 1, DtPagamento)) = 1 THEN 31 ELSE DAY(DtPagamento) END AS Dia
        INTO #DatasPagamentoCrm
        FROM #PagamentosCrm
        GROUP BY CodCli, DtPagamento;

        SELECT CodCli, MesPagamento, COUNT(1) AS QtdDatas,
               CASE WHEN DATEDIFF(MONTH, MesPagamento, @HistoricoReferencia) <= 2 THEN 3
                    WHEN DATEDIFF(MONTH, MesPagamento, @HistoricoReferencia) <= 5 THEN 2
                    ELSE 1 END AS PesoMes
        INTO #MesesPagamentoCrm
        FROM #DatasPagamentoCrm
        GROUP BY CodCli, MesPagamento;

        CREATE UNIQUE CLUSTERED INDEX IX_MesesPagamentoCrm ON #MesesPagamentoCrm(CodCli, MesPagamento);

        SELECT CodCli, COUNT(1) AS QtdMeses, SUM(QtdDatas) AS QtdDatas
        INTO #AmostrasPagamentoCrm
        FROM #MesesPagamentoCrm
        GROUP BY CodCli;

        WITH Pedidos AS
        (
            SELECT c.CodCli, UPPER(LTRIM(RTRIM(c.Pedido))) AS Pedido,
                   CASE WHEN c.Pago IS NULL THEN 1 ELSE 0 END AS EmAberto,
                   0 AS EmTerceiros, 0 AS TerceirosEmAberto
            FROM CReceber c
            INNER JOIN #ClientesCrmPagina p ON p.Codigo = c.CodCli
            WHERE c.Pedido LIKE 'AC%'
            UNION ALL
            SELECT c.Codcli, UPPER(LTRIM(RTRIM(c.Pedido))),
                   CASE WHEN c.Recebimento IS NULL THEN 1 ELSE 0 END,
                   1, CASE WHEN c.Recebimento IS NULL THEN 1 ELSE 0 END
            FROM CReceberCob c
            INNER JOIN #ClientesCrmPagina p ON p.Codigo = c.Codcli
            WHERE NULLIF(LTRIM(RTRIM(c.Pedido)), '') IS NOT NULL
        ),
        PedidosDistintos AS
        (
            SELECT CodCli, Pedido, MAX(EmAberto) AS EmAberto,
                   MAX(EmTerceiros) AS EmTerceiros, MAX(TerceirosEmAberto) AS TerceirosEmAberto
            FROM Pedidos
            GROUP BY CodCli, Pedido
        )
        SELECT CodCli, COUNT(1) AS QtdEncaminhamentos,
               SUM(CASE WHEN Pedido LIKE 'AC%' THEN 1 ELSE 0 END) AS QtdAcordos,
               SUM(CASE WHEN Pedido LIKE 'AC%' AND EmAberto = 1 THEN 1 ELSE 0 END) AS QtdAcordosEmAberto,
               SUM(CASE WHEN Pedido LIKE 'AC%' AND EmAberto = 0 THEN 1 ELSE 0 END) AS QtdAcordosQuitados,
               SUM(EmTerceiros) AS QtdTerceiros,
               SUM(TerceirosEmAberto) AS QtdTerceirosEmAberto,
               SUM(CASE WHEN EmTerceiros = 1 AND TerceirosEmAberto = 0 THEN 1 ELSE 0 END) AS QtdTerceirosQuitados
        INTO #EncaminhamentosCrm
        FROM PedidosDistintos
        GROUP BY CodCli;

        SELECT p.*,
               ISNULL(e.QtdEncaminhamentos, 0) AS HistQtdEncaminhamentos,
               ISNULL(e.QtdAcordos, 0) AS HistQtdAcordos,
               ISNULL(e.QtdAcordosEmAberto, 0) AS HistQtdAcordosEmAberto,
               ISNULL(e.QtdAcordosQuitados, 0) AS HistQtdAcordosQuitados,
               ISNULL(e.QtdTerceiros, 0) AS HistQtdTerceiros,
               ISNULL(e.QtdTerceirosEmAberto, 0) AS HistQtdTerceirosEmAberto,
               ISNULL(e.QtdTerceirosQuitados, 0) AS HistQtdTerceirosQuitados,
               ISNULL(a.QtdPagas, 0) AS HistQtdPagas,
               ISNULL(a.QtdAtrasadas, 0) AS HistQtdAtrasadas,
               ISNULL(a.QtdSemVencimento, 0) AS HistQtdSemVencimento,
               ISNULL(a.TotalDiasAtraso, 0) AS HistTotalDiasAtraso,
               ISNULL(am.QtdDatas, 0) AS HistQtdDatas,
               ISNULL(am.QtdMeses, 0) AS HistQtdMeses
        FROM #ClientesCrmPagina p
        LEFT JOIN #EncaminhamentosCrm e ON e.CodCli = p.Codigo
        LEFT JOIN #AtrasosCrm a ON a.CodCli = p.Codigo
        LEFT JOIN #AmostrasPagamentoCrm am ON am.CodCli = p.Codigo
        ORDER BY p.RowNum;

        SELECT d.CodCli, d.Dia,
               SUM(CONVERT(decimal(28, 12), m.PesoMes) / m.QtdDatas) AS Peso,
               MAX(d.DtPagamento) AS UltimaData
        FROM #DatasPagamentoCrm d
        INNER JOIN #MesesPagamentoCrm m ON m.CodCli = d.CodCli AND m.MesPagamento = d.MesPagamento
        INNER JOIN #AmostrasPagamentoCrm am ON am.CodCli = d.CodCli AND am.QtdMeses >= 3
        GROUP BY d.CodCli, d.Dia
        ORDER BY d.CodCli, d.Dia;

        DROP TABLE #EncaminhamentosCrm;
        DROP TABLE #AmostrasPagamentoCrm;
        DROP TABLE #MesesPagamentoCrm;
        DROP TABLE #DatasPagamentoCrm;
        DROP TABLE #AtrasosCrm;
        DROP TABLE #PagamentosCrm;
        DROP TABLE #ClientesCrmPagina;
        """;
}

using System.Text.RegularExpressions;
using ApiGrupos.Services;
using Microsoft.Data.SqlClient;

internal static class SqlIntegration
{
    public static async Task RunAsync()
    {
        var connectionString = Environment.GetEnvironmentVariable("APIGRUPOS_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.WriteLine("SKIP: integracao SQL com parcelas e pedidos sinteticos requer APIGRUPOS_TEST_SQL_CONNECTION; apenas tabelas temporarias sao criadas.");
            return;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        const string fixture = """
            SET NOCOUNT ON;
            CREATE TABLE #ClientesCrmPagina (Codigo int, RowNum bigint);
            INSERT INTO #ClientesCrmPagina VALUES (1,1), (2,2), (3,3);
            CREATE TABLE #ReceberTeste (CodCli int, Pedido varchar(50), Pago bit, Baixa datetime, Vencimento datetime);
            CREATE TABLE #TerceirosTeste (Codcli int, Pedido varchar(50), Recebimento datetime, Vencimento datetime);
            INSERT INTO #ReceberTeste VALUES
              (1,'AC100',NULL,NULL,'20270101'), (1,'AC100',1,'20240110','20240101'),
              (1,'AC200',1,'20240110','20240101'), (1,'AC300',1,'20240110','20240101'),
              (1,'V1',1,'20260710','20260701'), (1,'V2',1,'20260810','20260810'),
              (1,'V3',1,'20260910','20260915'), (1,'V4',1,'20260910','20260901'),
              (1,'OLD',1,'20251018','20240101'),
              (2,'ACX',1,'20260901',NULL), (2,'LATE',1,'20261019 23:59:59','20261001'),
              (2,'FUTURE',1,'20261020','20261001');
            INSERT INTO #TerceirosTeste VALUES
              (1,'T100',NULL,'20200101'), (1,'T100','20260810','20260801'),
              (1,'T200','20240110','20240101'), (1,'AC300','20240110','20240101'),
              (1,'T300','20260910','20260901'), (1,NULL,'20260710','20260701');
            """;
        var query = ClienteCrmHistoricoSql.Consulta.Replace("CAST(GETDATE() AS date)", "CAST('20261019' AS date)");
        query = Regex.Replace(query, @"\bCReceberCob\b", "#TerceirosTeste");
        query = Regex.Replace(query, @"\bCReceber\b", "#ReceberTeste");
        await using var command = new SqlCommand(fixture + query, connection) { CommandTimeout = 90 };
        await using var reader = await command.ExecuteReaderAsync();
        var seen = 0;
        while (await reader.ReadAsync())
        {
            seen++;
            int Value(string name) => Convert.ToInt32(reader[name]);
            if (Value("Codigo") == 1 &&
                (Value("HistQtdEncaminhamentos") != 6 || Value("HistQtdAcordos") != 3 ||
                 Value("HistQtdAcordosEmAberto") != 1 || Value("HistQtdAcordosQuitados") != 2 ||
                 Value("HistQtdTerceiros") != 4 || Value("HistQtdTerceirosEmAberto") != 1 || Value("HistQtdTerceirosQuitados") != 3 ||
                 Value("HistQtdPagas") != 7 || Value("HistQtdAtrasadas") != 5 || Value("HistTotalDiasAtraso") != 45 ||
                 Value("HistQtdDatas") != 3 || Value("HistQtdMeses") != 3))
                throw new Exception("Integracao SQL: pedidos repetidos, AC antigos/abertos/quitados ou pagamentos incorretos.");
            if (Value("Codigo") == 2 && (Value("HistQtdPagas") != 2 || Value("HistQtdAtrasadas") != 1 || Value("HistQtdSemVencimento") != 1 || Value("HistTotalDiasAtraso") != 18))
                throw new Exception("Integracao SQL: janela de 12 meses, vencimento nulo ou ultimo dia incorretos.");
            if (Value("Codigo") == 3 && (Value("HistQtdPagas") != 0 || Value("HistQtdAcordos") != 0))
                throw new Exception("Integracao SQL: cliente sem historico deve preservar zeros.");
        }
        if (seen != 3 || !await reader.NextResultAsync() || !await reader.ReadAsync() ||
            Convert.ToInt32(reader[0]) != 1 || reader.GetInt32(1) != 10 || reader.GetDecimal(2) != 8m || await reader.ReadAsync())
            throw new Exception("Integracao SQL: datas repetidas devem ter um voto e cada mes deve ter peso proprio; meses insuficientes devem ser excluidos.");
        Console.WriteLine("PASS: integracao SQL real com tabelas temporarias valida pedidos, acordos antigos, janela, parcelas e ponderacao mensal.");
    }
}

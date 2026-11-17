using System.Data;
using System.Text.RegularExpressions;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.Data.SqlClient;

internal static class SqlIntegration
{
    public static async Task RunAsync(bool requested, (string Name, DashboardCommand Command)[] plans, Action<bool, string> check)
    {
        if (!requested)
        {
            Console.WriteLine("PENDENTE: esquema real e integracao SQL. Configure APIGRUPOS_TEST_SQL_CONNECTION e execute com --sql.");
            return;
        }
        var connectionString = Environment.GetEnvironmentVariable("APIGRUPOS_TEST_SQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) throw new Exception("--sql requer APIGRUPOS_TEST_SQL_CONNECTION (nao informe credenciais no chat).");
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using (var audit = new SqlCommand(await File.ReadAllTextAsync(Path.Combine(HttpSmoke.Root, "scripts", "conferir-dashboard-esquema.sql")), connection) { CommandTimeout = 60 })
        await using (var reader = await audit.ExecuteReaderAsync())
        {
            do
            {
                // Este script retorna somente metadados e contagens agregadas.
                while (await reader.ReadAsync())
                    Console.WriteLine(string.Join(" | ", Enumerable.Range(0, reader.FieldCount).Select(i => reader.IsDBNull(i) ? "NULL" : Convert.ToString(reader.GetValue(i)))));
            } while (await reader.NextResultAsync());
        }
        foreach (var (name, plan) in plans)
        {
            await using var command = new SqlCommand(plan.Sql, connection) { CommandTimeout = 60 };
            command.Parameters.AddRange(plan.Parameters.Select(p => (SqlParameter)((ICloneable)p).Clone()).ToArray());
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SchemaOnly);
            check(reader.FieldCount > 0, "Sem projecao no esquema real: " + name);
            Console.WriteLine("PASS: nomes e projecao contra esquema real: " + name);
        }
        await FixturesAsync(connection, check);
    }

    private static async Task FixturesAsync(SqlConnection connection, Action<bool, string> check)
    {
        await using (var setup = new SqlCommand(await File.ReadAllTextAsync(Path.Combine(HttpSmoke.Root, "tests", "ApiGrupos.Dashboard", "fixtures.sql")), connection)) await setup.ExecuteNonQueryAsync();
        var tables = new[] { "ContaReceber", "CReceber", "CReceberCob", "FormaPG", "Metas", "MetasVendedor", "Vendedores", "EntradaControle", "ProdutoBarras", "Transferencia", "TransferenciaControle", "Subgrupos", "Grupos", "ConsultaVenda", "Produtos", "centrada", "Clientes", "CobrancaTerceiros", "Secao", "Cor", "Numeracao", "Fornecedor", "Categoria" };
        string ReplaceTables(string sql)
        {
            foreach (var table in tables) sql = Regex.Replace(sql, $@"(?<![#\w]){table}\b", "#" + table, RegexOptions.IgnoreCase);
            return sql;
        }
        async Task<List<T>> Query<T>(DashboardCommand plan) where T : new()
        {
            await using var command = new SqlCommand(ReplaceTables(plan.Sql), connection) { CommandTimeout = 60 };
            command.Parameters.AddRange(plan.Parameters.ToArray());
            await using var reader = await command.ExecuteReaderAsync();
            var result = new List<T>();
            while (await reader.ReadAsync()) result.Add(DashboardReader.Read<T>(reader));
            return result;
        }
        var inicio = new DateTime(2026, 6, 10); var fim = inicio.AddDays(5);
        var recebimentos = await Query<DashboardRecebimento>(DashboardSql.Recebimentos(new() { DataInicio = inicio, DataFim = fim }));
        check(recebimentos.Count == 4 && recebimentos.Sum(x => x.Vlpago) == 100m && recebimentos.Sum(x => x.Valor) == 200m, "SQL: todas as formas, NULL, REC AG. e valores diferentes.");
        check(recebimentos.Any(x => x.FormaPagamento == "MAQ CIELO  2-10X") && recebimentos.Any(x => x.FormaPagamento == "MAQ STONE PIX") && recebimentos.Any(x => x.Pedido is null) && recebimentos.Any(x => x.Pedido == "REC AG."), "SQL: formas e pedidos preservados.");
        var vencimento = await Query<DashboardRecebimento>(DashboardSql.Recebimentos(new() { DataInicio = inicio, DataFim = fim, TipoData = "vencimento", Loja = 3, LojaRecebimento = 5 }));
        check(vencimento.Count == 1 && vencimento[0].Vlpago == 40m, "SQL: vencimento, baixa anterior a 2022 e lojas independentes.");
        var titulos = await Query<DashboardTitulo>(DashboardSql.Titulos(new()));
        check(titulos.Count == 4 && titulos.Any(x => x.DtPedido < new DateTime(2022, 1, 1)), "SQL: titulos antigos, pagos e sem corte de 70 dias.");
        var abertos = await Query<DashboardTitulo>(DashboardSql.Titulos(new() { Situacao = "baixa-nula" }));
        var pagoNulo = await Query<DashboardTitulo>(DashboardSql.Titulos(new() { Situacao = "pago-nulo" }));
        var pagoPreenchido = await Query<DashboardTitulo>(DashboardSql.Titulos(new() { Situacao = "pago-preenchido" }));
        check(abertos.Count == 2 && abertos.Any(x => x.Pago == false) && pagoNulo.Count == 2 && pagoNulo.Any(x => x.DtBaixa is not null) && pagoPreenchido.Count == 2, "SQL: Pago e Baixa sao independentes.");
        var metas = await Query<DashboardMetaLoja>(DashboardSql.MetasLojas(new() { Ano = 2026, Mes = 6 }));
        check(metas.Count == 2 && metas.Sum(x => x.Vendas) == 300m, "SQL: metas aditivas na mesma loja/mes.");
        var vendedores = await Query<DashboardVendedor>(DashboardSql.Vendedores(new()));
        var metasV = await Query<DashboardMetaVendedor>(DashboardSql.MetasVendedores(new()));
        check(vendedores.Count == 2 && metasV.Count == 1 && metasV[0].Loja == 3 && vendedores[0].Loja == 9, "SQL: vendedor historico e transferido nao perde a meta.");
        var confirmar = await Query<DashboardEntradaConfirmar>(DashboardSql.EntradasConfirmar(new()));
        check(confirmar.Count == 2 && confirmar.Sum(x => x.ValorTotal) == -20m && confirmar.Any(x => x.Data < new DateTime(2022, 1, 1)), "SQL: entrada antiga, quant negativa e vinculo por barras.");
        var transferencias = await Query<DashboardTransferencia>(DashboardSql.Transferencias(new()));
        check(transferencias.Count(x => x.OrigemRegistro == "TRANSFERENCIA") == 4 && transferencias.Count(x => x.OrigemRegistro == "CONTROLE") == 3, "SQL: UNION ALL e multiplicidade do JOIN preservados.");
        check(transferencias.Any(x => x.TipoTransferencia == "AUTOMATICA PENDENTE" && x.DataAuto is not null) && transferencias.Any(x => x.TipoTransferencia == "AUTOMATICA AUTORIZADA" && x.DataAuto is null), "SQL: classificacao depende de UsuarioAuto.");
        check(transferencias.Count(x => x.TipoTransferencia == "MANUAL EFETIVADA") == 1 && transferencias.Count(x => x.TipoTransferencia == "MANUAL PENDENTE") == 2, "SQL: controle usa UsuarioConfirma, inclusive transferencia existente nao confirmada.");
        var pendentes = await Query<DashboardTransferencia>(DashboardSql.Transferencias(new() { Origem = 5, SomentePendentes = true }));
        check(pendentes.Count == 3 && pendentes.Any(x => x.DataTransferencia < new DateTime(2026, 6, 1)), "SQL: fotografia inclui pendencias anteriores a junho.");
        var ativacoes = await Query<DashboardAtivacao>(DashboardSql.Ativacoes(new() { DataInicio = inicio, DataFim = fim }));
        check(ativacoes.Count == 2 && ativacoes.Sum(x => x.Quant) == 1 && ativacoes.All(x => x.CodCli == 2), "SQL: primeira compra historica, dois pedidos e cliente excluido.");
        var refs = await Query<DashboardReferencia>(DashboardSql.Referencias(new()));
        check(refs.Single(x => x.Referencia == "OLD").CodProd == "1" && refs.Single(x => x.Referencia == "OLD").Descricao == "SECAO", "SQL: menor codigo de referencia antiga e descricao da secao.");
        var vendas = await Query<DashboardVendaAgregada>(DashboardSql.VendasAgregado(new() { DataInicio = inicio, DataFim = fim }));
        check(vendas.Any(x => x.Quantidade == 0m && x.FaturamentoPbix == 2m) && vendas.Sum(x => x.FaturamentoPbix) == 92m && vendas.Sum(x => x.CustoTotalPbix) == 35m, "SQL: devolucoes, descontos, custo e receita residual com quantidade zero.");
        var entradas = await Query<DashboardEntradaAgregada>(DashboardSql.EntradasAgregado(new() { DataInicio = inicio, DataFim = fim }));
        check(entradas.Count == 1 && entradas[0].Quantidade == 3m && entradas[0].ValorVendaEntradaPbix == 60m && entradas[0].CustoTotalEntradaPbix == 12m, "SQL: deduplicacao global antes da janela, preservando nota e barras.");
        var terceiros = await Query<DashboardTerceiro>(DashboardSql.Terceiros(new() { DataInicio = inicio, DataFim = fim, Status = 0 }));
        check(terceiros.Count == 1 && terceiros[0].Valor == 80m, "SQL: terceiros INNER JOIN, Status=0 e sem restricao de Recebimento.");

        // Os quatro SELECTs reais de Subgrupos e suas contagens, com os mesmos dados.
        var source = await File.ReadAllTextAsync(Path.Combine(HttpSmoke.Root, "Controllers", "SubgruposController.cs"));
        var queries = Regex.Matches(source, "const string sql(?<kind>SemPaginacao|Paginado|Total) = @?\"(?<sql>.*?)\";", RegexOptions.Singleline);
        var group = false;
        var seen = 0;
        foreach (Match match in queries)
        {
            if (match.Groups["sql"].Value.Contains("@GrupoCodigo")) group = true;
            await using var cmd = new SqlCommand(ReplaceTables(match.Groups["sql"].Value), connection);
            cmd.Parameters.AddWithValue("@GrupoCodigo", 1); cmd.Parameters.AddWithValue("@RowStart", 1); cmd.Parameters.AddWithValue("@RowEnd", 100);
            if (match.Groups["kind"].Value == "Total")
                check(Convert.ToInt32(await cmd.ExecuteScalarAsync()) == (group ? 1 : 2), "SQL: contagem de subgrupos deve usar o mesmo JOIN.");
            else
            {
                await using var reader = await cmd.ExecuteReaderAsync(); var count = 0;
                while (await reader.ReadAsync()) { count++; check(Convert.ToInt32(reader["DepartamentoCodigo"]) == 2, "SQL: sg.grupo e departamento numerico."); }
                check(count == (group ? 1 : 2), "SQL: subgrupos paginados e completos devem coincidir."); seen++;
            }
        }
        check(seen == 4, "Quatro caminhos de Subgrupos devem ser exercitados.");
        Console.WriteLine("PASS: integracao de consultas com fixtures sinteticas em tabelas temporarias; nenhuma tabela do ERP alterada.");
    }
}

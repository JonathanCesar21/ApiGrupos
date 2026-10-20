using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using ApiGrupos.Controllers;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using Dia = ApiGrupos.Services.ClienteCrmHistoricoCalculos.DiaPagamento;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}

var historico = new ClienteCrmHistorico();
ClienteCrmHistoricoCalculos.AplicarAtrasos(historico, 20, 5, 0, 90);
Check(historico.MediaDiasAtraso12Meses == 18 && historico.PercentualParcelasPagasComAtraso12Meses == 25,
    "Media deve considerar apenas as atrasadas; percentual deve considerar todas as pagas.");
ClienteCrmHistoricoCalculos.AplicarAtrasos(historico, 3, 1, 0, 2);
Check(historico.MediaDiasAtraso12Meses == 2 && historico.PercentualParcelasPagasComAtraso12Meses == 33.33m, "Arredondamento incorreto.");
ClienteCrmHistoricoCalculos.AplicarAtrasos(historico, 0, 0, 0, 0);
Check(historico.MediaDiasAtraso12Meses is null && historico.PercentualParcelasPagasComAtraso12Meses is null, "Sem pagamentos, nao inferir pontualidade.");
ClienteCrmHistoricoCalculos.AplicarAtrasos(historico, 10, 0, 0, 0);
Check(historico.MediaDiasAtraso12Meses == 0 && historico.PercentualParcelasPagasComAtraso12Meses == 0, "Pagamentos pontuais devem produzir zero.");
ClienteCrmHistoricoCalculos.AplicarAtrasos(historico, 10, 2, 1, 36);
Check(historico.MediaDiasAtraso12Meses == 18 && historico.PercentualParcelasPagasComAtraso12Meses is null, "Vencimentos desconhecidos nao podem ser classificados como pontuais.");

var recent = new DateTime(2026, 10, 10);
historico.QtdMesesComPagamento12Meses = 3;
ClienteCrmHistoricoCalculos.AplicarDiaHabitual(historico, new[] { new Dia(8, 1, recent), new Dia(10, 1, recent), new Dia(12, 1, recent) });
Check(historico.DiaHabitualPagamento == 10 && historico.ConcentracaoDiaHabitualPagamento == 100, "Habito deve encontrar o centro da faixa, nao a media de todo o mes.");
ClienteCrmHistoricoCalculos.AplicarDiaHabitual(historico, new[] { new Dia(31, 1, recent), new Dia(1, 1, recent), new Dia(2, 1, recent) });
Check(historico.DiaHabitualPagamento == 1, "Virada do mes deve ser tratada como faixa continua.");
ClienteCrmHistoricoCalculos.AplicarDiaHabitual(historico, new[] { new Dia(5, 1, recent), new Dia(20, 1, recent) });
Check(historico.DiaHabitualPagamento is null && historico.ConcentracaoDiaHabitualPagamento == 50, "Padrao disperso nao deve gerar dia habitual.");
ClienteCrmHistoricoCalculos.AplicarDiaHabitual(historico, new[] { new Dia(10, 6, recent), new Dia(25, 3, recent.AddMonths(-8)) });
Check(historico.DiaHabitualPagamento == 10 && historico.ConcentracaoDiaHabitualPagamento == 66.67m, "Peso recente deve prevalecer sem ocultar a dispersao.");
ClienteCrmHistoricoCalculos.AplicarDiaHabitual(historico, new[] { new Dia(31, 3, recent) });
Check(historico.DiaHabitualPagamento == 31, "Fim de mes normalizado deve continuar no fim do mes.");
historico.QtdMesesComPagamento12Meses = 2;
ClienteCrmHistoricoCalculos.AplicarDiaHabitual(historico, new[] { new Dia(10, 100, recent) });
Check(historico.DiaHabitualPagamento is null && historico.ConcentracaoDiaHabitualPagamento is null, "Muitas parcelas ou datas em poucos meses nao bastam para inferir um habito.");

// Exercita o mapeamento de producao, inclusive os campos antigos e os dois resultados SQL.
var table = new DataTable();
for (var i = 0; i < 28; i++) table.Columns.Add("Base" + i, typeof(object));
table.Columns.Add("RowNum", typeof(object));
foreach (var name in new[] { "HistQtdEncaminhamentos", "HistQtdAcordos", "HistQtdAcordosEmAberto", "HistQtdAcordosQuitados",
    "HistQtdTerceiros", "HistQtdTerceirosEmAberto", "HistQtdTerceirosQuitados", "HistQtdPagas", "HistQtdAtrasadas",
    "HistQtdSemVencimento", "HistTotalDiasAtraso", "HistQtdDatas", "HistQtdMeses" }) table.Columns.Add(name, typeof(object));
var row = table.NewRow();
row[0] = 2021001; row[1] = "Cliente de teste"; row[5] = "Mulher";
row[15] = 1; row[16] = 100m; row[17] = 2; row[18] = 200m; row[19] = 3; row[20] = 300m; row[21] = true;
row[27] = "19/10/2026"; row[28] = 1L;
var extras = new object[] { 6, 3, 1, 2, 4, 1, 3, 20, 5, 0, 90L, 3, 3 };
for (var i = 0; i < extras.Length; i++) row[i + 29] = extras[i];
table.Rows.Add(row);
var read = typeof(ClientesCrmController).GetMethod("ReadClienteCrmIndicadores", BindingFlags.NonPublic | BindingFlags.Static)!;
using var reader = table.CreateDataReader();
reader.Read();
var cliente = (ClienteCrmIndicadores)read.Invoke(null, new object[] { reader, true })!;
Check(cliente.Codigo == 2021001 && cliente.DtCadastro == "19/10/2026" && cliente.TotalEmAbertoTerceiros == 200m, "Mapeamento antigo deve permanecer intacto.");
Check(cliente.Historico is { QtdEncaminhamentosHistorico: 6, QtdAcordosHistorico: 3, QtdAcordosEmAberto: 1, QtdAcordosQuitados: 2,
    QtdEncaminhamentosTerceirosHistorico: 4, QtdEncaminhamentosTerceirosEmAberto: 1, QtdEncaminhamentosTerceirosQuitados: 3,
    MediaDiasAtraso12Meses: 18, PercentualParcelasPagasComAtraso12Meses: 25 }, "Mapeamento de acordos, terceiros ou atrasos incorreto.");
var cliente2 = new ClienteCrmIndicadores { Codigo = 2021002, Historico = new() { QtdMesesComPagamento12Meses = 3, QtdDatasPagamento12Meses = 3 } };
var dayTable = new DataTable();
dayTable.Columns.Add("CodCli", typeof(int)); dayTable.Columns.Add("Dia", typeof(int));
dayTable.Columns.Add("Peso", typeof(decimal)); dayTable.Columns.Add("UltimaData", typeof(DateTime));
dayTable.Rows.Add(2021001, 8, 1m, recent); dayTable.Rows.Add(2021001, 10, 1m, recent); dayTable.Rows.Add(2021001, 12, 1m, recent);
dayTable.Rows.Add(2021002, 20, 3m, recent);
using var dayReader = dayTable.CreateDataReader();
var readDays = typeof(ClientesCrmController).GetMethod("ReadDiasHabituaisAsync", BindingFlags.NonPublic | BindingFlags.Static)!;
await (Task)readDays.Invoke(null, new object[] { dayReader, new List<ClienteCrmIndicadores> { cliente, cliente2 }, CancellationToken.None })!;
Check(cliente.Historico!.DiaHabitualPagamento == 10 && cliente2.Historico!.DiaHabitualPagamento == 20, "Historicos nao podem ser misturados entre clientes; o ultimo grupo precisa ser aplicado.");
var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
using var json = JsonDocument.Parse(JsonSerializer.Serialize(cliente, jsonOptions));
Check(json.RootElement.GetProperty("historico").GetProperty("mediaDiasAtraso12Meses").GetDecimal() == 18, "Contrato JSON do historico incorreto.");
var antigo = (ClienteCrmIndicadores)read.Invoke(null, new object[] { reader, false })!;
using var jsonAntigo = JsonDocument.Parse(JsonSerializer.Serialize(antigo, jsonOptions));
Check(!jsonAntigo.RootElement.TryGetProperty("historico", out _), "incluirHistorico=false deve preservar o contrato e tamanho anteriores.");

new TSql100Parser(true).Parse(new StringReader(ClienteCrmHistoricoSql.Consulta), out var errors);
Check(errors.Count == 0, "Sintaxe SQL Server 2008 invalida: " + string.Join("; ", errors.Select(error => error.Message)));
var directory = new DirectoryInfo(AppContext.BaseDirectory);
while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ApiGrupos.csproj"))) directory = directory.Parent;
var source = File.ReadAllText(Path.Combine(directory!.FullName, "Controllers", "ClientesCrmController.cs"));
var template = Regex.Match(source, "var sql = \\$\"\"\"(?<sql>.*?)\"\"\";", RegexOptions.Singleline);
Check(template.Success, "Consulta principal nao encontrada para verificacao de sintaxe.");
var cte = (string)typeof(ClientesCrmController).GetMethod("BuildCte", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;
foreach (var include in new[] { true, false })
{
    var fullSql = template.Groups["sql"].Value.Replace("{cte}", cte).Replace("{where}", "WHERE db.Codigo = @CodCli")
        .Replace("{destinoHistorico}", include ? ", RowNum\nINTO #ClientesCrmPagina" : string.Empty)
        .Replace("{(incluirHistorico ? ClienteCrmHistoricoSql.Consulta : string.Empty)}", include ? ClienteCrmHistoricoSql.Consulta : string.Empty);
    new TSql100Parser(true).Parse(new StringReader(fullSql), out var fullErrors);
    Check(fullErrors.Count == 0, "Consulta completa invalida: " + string.Join("; ", fullErrors.Select(error => error.Message)));
}
var controller = new ClientesCrmController(new ConnectionStringProvider("Server=127.0.0.1;Database=Unused;Encrypt=False;"));
foreach (var incluir in new[] { true, false })
    Check(await controller.Get(new PaginacaoQuery(), incluirHistorico: incluir) is ObjectResult { StatusCode: 503 }, "Historico opcional deve preservar o erro de credenciais ausentes.");

Console.WriteLine($"PASS: {checks} verificacoes de calculos, habitos, isolamento entre clientes, mapeamento, JSON, sintaxe SQL e modo sem historico.");
await SqlIntegration.RunAsync();

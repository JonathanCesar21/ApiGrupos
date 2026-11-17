using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using ApiGrupos.Controllers;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

var checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
}
void Invalid(Action action, string message)
{
    try { action(); } catch (ArgumentException) { checks++; return; }
    throw new Exception(message);
}
var start = new DateTime(2026, 6, 10);
DashboardPeriodoQuery Period() => new() { DataInicio = start, DataFim = start.AddDays(5) };
var recebimentoQuery = new DashboardRecebimentosQuery { DataInicio = start, DataFim = start.AddDays(119), Loja = 3, LojaRecebimento = 5 };
var baixa = DashboardSql.Recebimentos(recebimentoQuery);
Check(baixa.Parameters.Single(p => p.ParameterName == "@DataFimExclusivo").Value.Equals(start.AddDays(120)), "120 dias inclusivos devem ser aceitos.");
Check(baixa.Sql.Contains("c.Baixa >= '20220101'") && !baixa.Sql.Contains("REC AG.") && !baixa.Sql.Contains("fp.Forma IN"), "Recebimentos devem manter todas as formas/pedidos e o universo desde 2022.");
Check(baixa.Parameters.Single(p => p.ParameterName == "@Loja").Value.Equals(3) && baixa.Parameters.Single(p => p.ParameterName == "@LojaRecebimento").Value.Equals(5), "Loja e loja de recebimento sao filtros diferentes.");
recebimentoQuery.TipoData = "vencimento";
Check(DashboardSql.Recebimentos(recebimentoQuery).Sql.Contains("c.Vencimento >= @DataInicio"), "Filtro por vencimento incorreto.");
recebimentoQuery.DataFim = start.AddDays(120);
Invalid(() => DashboardSql.Recebimentos(recebimentoQuery), "121 dias nao podem ser aceitos.");
Invalid(() => DashboardSql.VendasAgregado(new()), "Periodo de evento obrigatorio.");
Invalid(() => DashboardSql.Titulos(new() { DataInicio = start }), "Periodo parcial nao pode ser ignorado.");
Invalid(() => DashboardSql.Ativacoes(new() { DataInicio = start, DataFim = start.AddDays(-1) }), "Periodo invertido deve falhar.");
Invalid(() => DashboardSql.EntradasConfirmar(new() { DataInicio = DateTime.MinValue, DataFim = start }), "Data anterior ao datetime SQL deve falhar.");
Invalid(() => DashboardSql.Transferencias(new() { DataInicio = start, DataFim = DateTime.MaxValue }), "Data maxima deve falhar sem overflow.");
Invalid(() => DashboardSql.Recebimentos(new() { TipoData = "baixa; DROP TABLE x", DataInicio = start, DataFim = start }), "tipoData deve usar allowlist.");
Invalid(() => DashboardSql.Titulos(new() { Situacao = "Pago=1" }), "situacao deve usar allowlist.");
Invalid(() => DashboardSql.Transferencias(new() { TipoTransferencia = "qualquer" }), "Tipo de transferencia invalido.");
Invalid(() => DashboardSql.MetasLojas(new() { Mes = 13 }), "Mes invalido.");
Invalid(() => DashboardSql.MetasVendedores(new() { Ano = 2021 }), "Metas anteriores a 2022 invalidas.");
Invalid(() => DashboardSql.Vendedores(new() { Loja = -1 }), "Loja negativa invalida.");
var snapshot = DashboardSql.Titulos(new());
Check(!snapshot.Sql.Contains("DATEDIFF") && !snapshot.Sql.Contains("2022") && !snapshot.Sql.Contains("Pago IS"), "Titulos nao podem herdar cortes legados.");
Check(DashboardSql.Titulos(new() { Situacao = "baixa-nula" }).Sql.Contains("c.Baixa IS NULL"), "Aberto PBIX usa Baixa IS NULL.");
Check(DashboardSql.Titulos(new() { Situacao = "pago-nulo" }).Sql.Contains("c.Pago IS NULL"), "Pago nulo e filtro separado.");
Check(DashboardSql.Titulos(new() { Situacao = "pago-preenchido" }).Sql.Contains("c.Pago IS NOT NULL"), "Pago preenchido nao significa Pago=true.");
Check(DashboardSql.EntradasConfirmar(new()).Parameters.Count == 0, "Fotografia de entradas deve incluir pendencias antigas.");
Check(DashboardSql.Transferencias(new()).Parameters.Count == 0, "Fotografia de transferencias nao exige periodo.");
var injection = "26-REF'; DROP TABLE Produtos;--";
var reference = DashboardSql.Referencias(new() { Referencia = injection });
Check(!reference.Sql.Contains(injection) && reference.Parameters.Single().Value.Equals(injection), "Referencia deve ser parametrizada sem remover prefixos.");
Check(!reference.Sql.Contains("2025") && reference.Sql.Contains("PARTITION BY p.referencia ORDER BY p.codigo"), "Referencia deve escolher o menor codigo sem corte de cadastro.");
Check(DashboardSql.MetasVendedores(new()).Sql.Contains("v.codvend = mv.CodVend") && !DashboardSql.MetasVendedores(new()).Sql.Contains("v.Loja = mv.Loja"), "Transferencia de vendedor entre lojas nao pode excluir meta.");
Check(!DashboardSql.MetasLojas(new()).Sql.Contains("DISTINCT"), "Metas aditivas nao podem ser deduplicadas.");
var thirds = DashboardSql.Terceiros(new() { DataInicio = start, DataFim = start, Status = 0 });
Check(thirds.Sql.Contains("INNER JOIN CobrancaTerceiros") && !thirds.Sql.Contains("Recebimento IS NULL"), "Terceiros deve reproduzir universo do PBIX.");

var plans = new (string Name, DashboardCommand Command)[]
{
    ("recebimentos", DashboardSql.Recebimentos(new() { DataInicio = start, DataFim = start })),
    ("titulos", snapshot), ("metas-lojas", DashboardSql.MetasLojas(new())),
    ("metas-vendedores", DashboardSql.MetasVendedores(new())), ("vendedores", DashboardSql.Vendedores(new())),
    ("entradas-a-confirmar", DashboardSql.EntradasConfirmar(new())), ("transferencias", DashboardSql.Transferencias(new())),
    ("ativacoes", DashboardSql.Ativacoes(Period())), ("referencias", DashboardSql.Referencias(new())),
    ("terceiros", thirds), ("vendas-agregado", DashboardSql.VendasAgregado(Period())),
    ("entradas-agregado", DashboardSql.EntradasAgregado(Period()))
};
foreach (var (name, plan) in plans)
{
    foreach (var parser in new TSqlParser[] { new TSql90Parser(true), new TSql100Parser(true) })
    {
        var fragment = parser.Parse(new StringReader(plan.Sql), out var errors);
        Check(errors.Count == 0, $"SQL {name}: {string.Join("; ", errors.Select(e => e.Message))}");
        var visitor = new Functions(); fragment.Accept(visitor);
        Check(visitor.Errors.Count == 0, $"Funcoes SQL antigas {name}: {string.Join(", ", visitor.Errors)}");
    }
}

var dtoTypes = new Dictionary<string, Type>
{
    ["recebimentos"] = typeof(DashboardRecebimento), ["titulos"] = typeof(DashboardTitulo),
    ["metas-lojas"] = typeof(DashboardMetaLoja), ["metas-vendedores"] = typeof(DashboardMetaVendedor),
    ["vendedores"] = typeof(DashboardVendedor), ["entradas-a-confirmar"] = typeof(DashboardEntradaConfirmar),
    ["transferencias"] = typeof(DashboardTransferencia), ["ativacoes"] = typeof(DashboardAtivacao),
    ["referencias"] = typeof(DashboardReferencia), ["terceiros"] = typeof(DashboardTerceiro),
    ["vendas-agregado"] = typeof(DashboardVendaAgregada), ["entradas-agregado"] = typeof(DashboardEntradaAgregada)
};
foreach (var (name, plan) in plans)
{
    var fragment = (TSqlScript)new TSql90Parser(true).Parse(new StringReader(plan.Sql), out _);
    var select = (SelectStatement)fragment.Batches.Single().Statements.Single();
    var specification = (QuerySpecification)select.QueryExpression;
    var projected = specification.SelectElements.Cast<SelectScalarExpression>().Select(e =>
        e.ColumnName?.Value ?? ((ColumnReferenceExpression)e.Expression).MultiPartIdentifier.Identifiers.Last().Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
    var writable = dtoTypes[name].GetProperties().Where(p => p.CanWrite).Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
    Check(projected.SetEquals(writable), $"Colunas projetadas e DTO divergem em {name}: {string.Join(",", projected.Except(writable).Concat(writable.Except(projected)))}");
}

foreach (var file in new[] { Path.Combine("tests", "ApiGrupos.Dashboard", "fixtures.sql"), Path.Combine("scripts", "conferir-dashboard-esquema.sql") })
foreach (var parser in new TSqlParser[] { new TSql90Parser(true), new TSql100Parser(true) })
{
    parser.Parse(new StringReader(File.ReadAllText(Path.Combine(HttpSmoke.Root, file))), out var errors);
    Check(errors.Count == 0, $"Script {file} incompativel: {string.Join("; ", errors.Select(e => e.Message))}");
}
var subgroupSource = File.ReadAllText(Path.Combine(HttpSmoke.Root, "Controllers", "SubgruposController.cs"));
var subgroupQueries = Regex.Matches(subgroupSource, "const string sql(?<kind>SemPaginacao|Paginado|Total) = @?\"(?<sql>.*?)\";", RegexOptions.Singleline);
Check(subgroupQueries.Count == 6, "As duas contagens e os quatro caminhos de Subgrupos devem existir.");
foreach (Match query in subgroupQueries)
{
    new TSql90Parser(true).Parse(new StringReader(query.Groups["sql"].Value), out var errors);
    Check(errors.Count == 0, "Sintaxe de Subgrupos incorreta: " + string.Join("; ", errors.Select(e => e.Message)));
}

T Row<T>(params (string Name, object? Value)[] columns) where T : new()
{
    var table = new DataTable();
    foreach (var c in columns) table.Columns.Add(c.Name, typeof(object));
    table.Rows.Add(columns.Select(c => c.Value ?? DBNull.Value).ToArray());
    using var reader = table.CreateDataReader(); reader.Read();
    return DashboardReader.Read<T>(reader);
}
var receipt = Row<DashboardRecebimento>(("codFormaPagamento", 10), ("FormaPagamento", " DINHEIRO"), ("Valor", 120m), ("vlpago", 90m), ("Pedido", null), ("Loja", "03"), ("LojaRecebimento", 5), ("Baixa", start));
Check(receipt is { Valor: 120m, Vlpago: 90m, Pedido: null, Loja: 3, LojaRecebimento: 5, FormaPagamento: " DINHEIRO" }, "Mapeamento preserva valores distintos, pedido nulo e espaco da forma.");
Check(receipt.Baixa!.Value.Kind == DateTimeKind.Unspecified, "Data de negocio deve ser sem conversao UTC.");
foreach (var forma in new[] { "MAQ CIELO  2-10X", "MAQ STONE PIX", "CHEQUE PRÉ" })
    Check(Row<DashboardRecebimento>(("FormaPagamento", forma)).FormaPagamento == forma, "Forma nao pode ser normalizada.");
Check(Row<DashboardTitulo>(("Pago", false), ("DtBaixa", null)).Pago == false, "Pago=false nao e NULL.");
Check(Row<DashboardTitulo>(("Pago", null), ("DtBaixa", start)).Pago is null, "Pago=NULL e baixa preenchida preservados.");
Check(Row<DashboardEntradaConfirmar>(("Quant", -2.5m), ("Valor", 4m), ("Barras", "000123")).ValorTotal == -10m, "Quantidade fracionaria/negativa e barras textuais.");
Check(Row<DashboardEntradaConfirmar>(("Quant", 2m), ("Valor", null)).ValorTotal is null, "Custo NULL nao vira zero.");
Check(Row<DashboardMetaLoja>(("Receber", 100m), ("Terceiros", 30m)).ReceberMaisTerceiros == 130m, "Meta de recebimentos e terceiros aditiva.");
Check(Row<DashboardMetaLoja>(("Receber", 100m), ("Terceiros", null)).ReceberMaisTerceiros is null, "Metas NULL preservadas.");
Check(Row<DashboardMetaLoja>(("Mes", null), ("Data", null)).Data is null, "Mes e data nulos na origem devem ser preservados.");
foreach (var (value, expected) in new (decimal?, int)[] { (1m, 1), (0m, 0), (-1m, 0), (null, 0) })
    Check(Row<DashboardAtivacao>(("Valor", value)).Quant == expected, "Ativacao quant depende de valor positivo.");
foreach (var (printed, transferType, expected) in new (bool?, string, string?)[]
{
    (true, "MANUAL", "Preparado Para Envio"), (false, "MANUAL PENDENTE", "Não Preparado"),
    (null, "MANUAL PENDENTE", "Preparado Para Envio"), (null, "MANUAL EFETIVADA", null)
}) Check(Row<DashboardTransferencia>(("StatusImpressao", printed), ("TipoTransferencia", transferType)).Status == expected, "Status de preparo incorreto.");
var transfer = Row<DashboardTransferencia>(("DescSubGrupo", "MOVEIS"), ("DescSecao", null), ("DescNumero", ""), ("DescCor", "AZUL"), ("Automatica", true));
Check(transfer.ProdutoCompleto == "MOVEIS -  - AZUL" && transfer.Tipo == "Automática", "Text.Combine ignora NULL e preserva vazio.");
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
using var json = JsonDocument.Parse(JsonSerializer.Serialize(receipt, options));
Check(json.RootElement.GetProperty("pedido").ValueKind == JsonValueKind.Null && json.RootElement.GetProperty("loja").GetInt32() == 3 && !json.RootElement.GetProperty("baixa").GetString()!.EndsWith('Z'), "JSON camelCase, NULL, loja e data sem UTC.");

var controller = new DashboardController(new ConnectionStringProvider(null), NullLogger<DashboardController>.Instance);
Check(await controller.Recebimentos(new()) is BadRequestObjectResult, "Validacao ocorre antes da conexao.");
Check(await controller.Titulos(new()) is ObjectResult { StatusCode: 503 }, "Banco nao configurado deve dar 503, nunca array vazio.");
using var canceled = new CancellationTokenSource(); canceled.Cancel();
Check(await controller.Titulos(new(), canceled.Token) is EmptyResult, "Cancelamento nao vira 500.");
controller.ControllerContext = new() { HttpContext = new DefaultHttpContext() };
controller.Request.QueryString = new QueryString("?page=1");
Check(await controller.Titulos(new()) is BadRequestObjectResult, "Paginacao nao suportada deve ser rejeitada.");

await HttpSmoke.RunAsync(args.Contains("--export-openapi"), Check);
await SqlIntegration.RunAsync(args.Contains("--sql"), plans, Check);
Console.WriteLine($"PASS: {checks} verificacoes dashboard (contratos, filtros, SQL, mapeamento, HTTP e autenticacao).");

sealed class Functions : TSqlFragmentVisitor
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase) { "DATEADD", "DATEDIFF", "MIN", "SUM", "ROW_NUMBER" };
    public List<string> Errors { get; } = [];
    public override void ExplicitVisit(FunctionCall node)
    {
        if (!Allowed.Contains(node.FunctionName.Value)) Errors.Add(node.FunctionName.Value);
        base.ExplicitVisit(node);
    }
}

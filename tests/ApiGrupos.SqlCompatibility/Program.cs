using System.Text.RegularExpressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

var directory = new DirectoryInfo(AppContext.BaseDirectory);
while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ApiGrupos.csproj")))
{
    directory = directory.Parent;
}

if (directory is null)
{
    throw new InvalidOperationException("Nao foi possivel localizar a raiz do projeto.");
}

var controllerPath = Path.Combine(directory.FullName, "Controllers", "ConsultaVendasTransferenciasAutomaticasController.cs");
var source = File.ReadAllText(controllerPath);
var sqlMatch = Regex.Match(source, "const string sql = \"\"\"(?<sql>.*?)\"\"\";", RegexOptions.Singleline);
if (!sqlMatch.Success)
{
    throw new InvalidOperationException("A consulta SQL da rota nao foi encontrada.");
}

var sql = sqlMatch.Groups["sql"].Value;
foreach (var (version, parser) in new (string, TSqlParser)[]
{
    ("SQL Server 2005", new TSql90Parser(true)),
    ("SQL Server 2008", new TSql100Parser(true))
})
{
    var errors = ValidateSql(sql, parser);
    if (errors.Count > 0)
    {
        throw new InvalidOperationException($"Consulta incompativel com {version}: {string.Join("; ", errors)}");
    }

    Console.WriteLine($"PASS: sintaxe e funcoes da rota compativeis com a verificacao de {version}.");

    // O parser aceita nomes de funcoes desconhecidas. Esta regressao confirma
    // que a verificacao adicional detecta a causa do erro ocorrido no ERP.
    var regressionErrors = ValidateSql("SELECT TRY_CONVERT(int, '03');", parser);
    if (!regressionErrors.Any(error => error.Contains("TRY_CONVERT", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidOperationException("A regressao nao detectou a funcao TRY_CONVERT.");
    }
}

Console.WriteLine("PASS: regressao detecta TRY_CONVERT mesmo quando a sintaxe e aceita pelo parser.");

static List<string> ValidateSql(string sql, TSqlParser parser)
{
    var fragment = parser.Parse(new StringReader(sql), out var parseErrors);
    var errors = parseErrors.Select(error => $"Linha {error.Line}: {error.Message}").ToList();
    if (parseErrors.Count == 0)
    {
        var functions = new LegacyFunctionsVisitor();
        fragment.Accept(functions);
        errors.AddRange(functions.Errors);
    }

    return errors;
}

sealed class LegacyFunctionsVisitor : TSqlFragmentVisitor
{
    // Funcoes usadas por esta rota e disponiveis no SQL Server 2005/2008.
    // Novas funcoes devem ter sua disponibilidade confirmada antes de entrar
    // nesta lista; o parser sozinho nao valida funcoes internas do servidor.
    private static readonly HashSet<string> AllowedFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "COUNT", "MIN", "LTRIM", "RTRIM", "NULLIF"
    };

    public List<string> Errors { get; } = new();

    public override void ExplicitVisit(FunctionCall node)
    {
        if (!AllowedFunctions.Contains(node.FunctionName.Value))
        {
            Errors.Add($"Funcao nao verificada para SQL Server 2005/2008: {node.FunctionName.Value}.");
        }

        base.ExplicitVisit(node);
    }
}

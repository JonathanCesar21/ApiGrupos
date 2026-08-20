using System.Data;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/clientes/crm")]
public class ClientesCrmController : ControllerBase
{
    private const int CommandTimeoutSeconds = 90;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ClientesCrmController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get(
        [FromQuery] PaginacaoQuery paginacao,
        [FromQuery] string? busca = null,
        [FromQuery] int? codCli = null,
        [FromQuery] int? loja = null,
        [FromQuery] int? codGrupo = null,
        [FromQuery] string? sexo = null,
        [FromQuery] decimal? rendaMin = null,
        [FromQuery] decimal? rendaMax = null,
        [FromQuery] decimal? limiteDispMin = null,
        [FromQuery] decimal? limiteDispMax = null,
        [FromQuery] bool somenteSemParcelasEmAberto = false,
        [FromQuery] bool somenteComParcelasEmAberto = false,
        [FromQuery] bool comTerceiros = false,
        [FromQuery] bool semTerceiros = false,
        [FromQuery] DateTime? quitacaoDe = null,
        [FromQuery] DateTime? quitacaoAte = null,
        CancellationToken cancellationToken = default)
    {
        if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
        {
            return BadRequest(error);
        }

        if (somenteSemParcelasEmAberto && somenteComParcelasEmAberto)
        {
            return BadRequest("Use apenas um dos filtros: 'somenteSemParcelasEmAberto' ou 'somenteComParcelasEmAberto'.");
        }

        if (comTerceiros && semTerceiros)
        {
            return BadRequest("Use apenas um dos filtros: 'comTerceiros' ou 'semTerceiros'.");
        }

        if (rendaMin.HasValue && rendaMax.HasValue && rendaMax.Value < rendaMin.Value)
        {
            return BadRequest("O parametro 'rendaMax' deve ser maior ou igual a 'rendaMin'.");
        }

        if (limiteDispMin.HasValue && limiteDispMax.HasValue && limiteDispMax.Value < limiteDispMin.Value)
        {
            return BadRequest("O parametro 'limiteDispMax' deve ser maior ou igual a 'limiteDispMin'.");
        }

        if (quitacaoDe.HasValue && quitacaoAte.HasValue && quitacaoAte.Value.Date < quitacaoDe.Value.Date)
        {
            return BadRequest("O parametro 'quitacaoAte' deve ser maior ou igual a 'quitacaoDe'.");
        }

        if (!TryResolveSexoFilter(sexo, out var sexoFiltro, out error))
        {
            return BadRequest(error);
        }

        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var clientes = new List<ClienteCrmIndicadores>();
            var buscaNormalizada = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim();
            var hasCodigoBusca = int.TryParse(buscaNormalizada, out var codigoBusca);
            var quitacaoAteExclusivo = quitacaoAte?.Date.AddDays(1);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var where = BuildWhere(
                buscaNormalizada,
                hasCodigoBusca,
                codCli,
                loja,
                codGrupo,
                sexoFiltro,
                rendaMin,
                rendaMax,
                limiteDispMin,
                limiteDispMax,
                somenteSemParcelasEmAberto,
                somenteComParcelasEmAberto,
                comTerceiros,
                semTerceiros,
                quitacaoDe,
                quitacaoAteExclusivo);

            var cte = BuildCte();
            var sqlTotal = $"""
                {cte}
                SELECT COUNT(1)
                FROM DadosBase db
                {where}
                """;

            await using var commandTotal = new SqlCommand(sqlTotal, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };
            AddParameters(
                commandTotal,
                buscaNormalizada,
                hasCodigoBusca,
                codigoBusca,
                codCli,
                loja,
                codGrupo,
                sexoFiltro,
                rendaMin,
                rendaMax,
                limiteDispMin,
                limiteDispMax,
                quitacaoDe?.Date,
                quitacaoAteExclusivo);

            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync(cancellationToken));
            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            var sql = $"""
                {cte},
                Dados AS
                (
                    SELECT
                        db.*,
                        ROW_NUMBER() OVER
                        (
                            ORDER BY
                                db.DtUltimaQuitacaoCarne DESC,
                                db.Nome,
                                db.Codigo
                        ) AS RowNum
                    FROM DadosBase db
                    {where}
                )
                SELECT
                    Codigo,
                    Nome,
                    Bairro,
                    NomeCidade,
                    DtNascimento,
                    Sexo,
                    CodGrupo,
                    Limite,
                    Renda,
                    Idade,
                    Loja,
                    Fone,
                    Fwhats,
                    FoneReferencia1,
                    FoneReferencia2,
                    QtdEmAbertoCrediario,
                    TotalEmAbertoCrediario,
                    QtdEmAbertoTerceiros,
                    TotalEmAbertoTerceiros,
                    QtdParcelasEmAberto,
                    TotalEmAberto,
                    TemParcelasEmAberto,
                    LimiteDisponivel,
                    DtVencimentoMaisAntigoEmAberto,
                    DiasMaiorAtraso,
                    DtUltimaBaixa,
                    DtUltimaQuitacaoCarne
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };
            AddParameters(
                command,
                buscaNormalizada,
                hasCodigoBusca,
                codigoBusca,
                codCli,
                loja,
                codGrupo,
                sexoFiltro,
                rendaMin,
                rendaMax,
                limiteDispMin,
                limiteDispMax,
                quitacaoDe?.Date,
                quitacaoAteExclusivo);
            command.Parameters.Add("@RowStart", SqlDbType.Int).Value = rowStart;
            command.Parameters.Add("@RowEnd", SqlDbType.Int).Value = rowEnd;

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                clientes.Add(ReadClienteCrmIndicadores(reader));
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<ClienteCrmIndicadores>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = clientes
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (SqlException ex) when (ex.Number == -2)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout,
                $"A consulta excedeu o timeout de {CommandTimeoutSeconds} segundos e foi cancelada.");
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar indicadores de clientes CRM no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar indicadores de clientes CRM: {ex.Message}");
        }
    }

    private static string BuildCte()
    {
        return """
            WITH ReceberAberto AS
            (
                SELECT
                    c.CodCli,
                    COUNT(1) AS QtdEmAbertoCrediario,
                    SUM(ISNULL(c.Valor, 0)) AS TotalEmAbertoCrediario,
                    MIN(c.Vencimento) AS DtVencimentoMaisAntigoCrediario
                FROM CReceber c
                WHERE c.Pago IS NULL
                GROUP BY c.CodCli
            ),
            TerceirosAberto AS
            (
                SELECT
                    c.Codcli AS CodCli,
                    COUNT(1) AS QtdEmAbertoTerceiros,
                    SUM(ISNULL(c.Valor, 0)) AS TotalEmAbertoTerceiros,
                    MIN(c.Vencimento) AS DtVencimentoMaisAntigoTerceiros
                FROM CReceberCob c
                WHERE c.Recebimento IS NULL
                GROUP BY c.Codcli
            ),
            Baixas AS
            (
                SELECT
                    c.CodCli,
                    MAX(c.Baixa) AS DtUltimaBaixa
                FROM CReceber c
                WHERE c.Pago IS NOT NULL
                  AND c.Baixa IS NOT NULL
                GROUP BY c.CodCli
            ),
            PedidosQuitados AS
            (
                SELECT
                    c.CodCli,
                    c.Pedido,
                    MAX(c.Baixa) AS DtQuitacaoPedido
                FROM CReceber c
                WHERE c.Pedido IS NOT NULL
                GROUP BY
                    c.CodCli,
                    c.Pedido
                HAVING SUM(CASE WHEN c.Pago IS NULL THEN 1 ELSE 0 END) = 0
                   AND MAX(c.Baixa) IS NOT NULL
                   AND NOT EXISTS
                   (
                       SELECT 1
                       FROM CReceberCob cob
                       WHERE cob.Codcli = c.CodCli
                         AND cob.Pedido = c.Pedido
                         AND cob.Recebimento IS NULL
                   )
            ),
            QuitacoesCarne AS
            (
                SELECT
                    CodCli,
                    MAX(DtQuitacaoPedido) AS DtUltimaQuitacaoCarne
                FROM PedidosQuitados
                GROUP BY CodCli
            ),
            DadosBase AS
            (
                SELECT
                    c.Codigo,
                    c.nome AS Nome,
                    c.Bairro,
                    cid.Cidade AS NomeCidade,
                    c.dtNascimento,
                    CASE c.Sexo
                        WHEN 0 THEN 'Homem'
                        WHEN 1 THEN 'Mulher'
                        ELSE 'Nao informado'
                    END AS Sexo,
                    c.CodGrupo,
                    c.Limite,
                    c.Renda,
                    c.Idade,
                    c.Loja,
                    c.Fone,
                    c.Fwhats,
                    c.FoneRef1 AS FoneReferencia1,
                    c.FoneRef2 AS FoneReferencia2,
                    ISNULL(ra.QtdEmAbertoCrediario, 0) AS QtdEmAbertoCrediario,
                    ISNULL(ra.TotalEmAbertoCrediario, 0) AS TotalEmAbertoCrediario,
                    ISNULL(ta.QtdEmAbertoTerceiros, 0) AS QtdEmAbertoTerceiros,
                    ISNULL(ta.TotalEmAbertoTerceiros, 0) AS TotalEmAbertoTerceiros,
                    totals.QtdParcelasEmAberto,
                    totals.TotalEmAberto,
                    CONVERT(bit, CASE WHEN totals.QtdParcelasEmAberto > 0 THEN 1 ELSE 0 END) AS TemParcelasEmAberto,
                    ISNULL(c.Limite, 0) - totals.TotalEmAberto AS LimiteDisponivel,
                    datas.DtVencimentoMaisAntigoEmAberto,
                    CASE
                        WHEN datas.DtVencimentoMaisAntigoEmAberto IS NULL THEN NULL
                        WHEN DATEDIFF(DAY, datas.DtVencimentoMaisAntigoEmAberto, CAST(GETDATE() AS date)) < 0 THEN 0
                        ELSE DATEDIFF(DAY, datas.DtVencimentoMaisAntigoEmAberto, CAST(GETDATE() AS date))
                    END AS DiasMaiorAtraso,
                    b.DtUltimaBaixa,
                    qc.DtUltimaQuitacaoCarne
                FROM Clientes c
                INNER JOIN Cidades cid
                    ON c.Cidade = cid.Seq
                LEFT JOIN ReceberAberto ra
                    ON c.Codigo = ra.CodCli
                LEFT JOIN TerceirosAberto ta
                    ON c.Codigo = ta.CodCli
                LEFT JOIN Baixas b
                    ON c.Codigo = b.CodCli
                LEFT JOIN QuitacoesCarne qc
                    ON c.Codigo = qc.CodCli
                CROSS APPLY
                (
                    SELECT
                        ISNULL(ra.QtdEmAbertoCrediario, 0) + ISNULL(ta.QtdEmAbertoTerceiros, 0) AS QtdParcelasEmAberto,
                        ISNULL(ra.TotalEmAbertoCrediario, 0) + ISNULL(ta.TotalEmAbertoTerceiros, 0) AS TotalEmAberto
                ) totals
                CROSS APPLY
                (
                    SELECT CASE
                        WHEN ra.DtVencimentoMaisAntigoCrediario IS NULL THEN ta.DtVencimentoMaisAntigoTerceiros
                        WHEN ta.DtVencimentoMaisAntigoTerceiros IS NULL THEN ra.DtVencimentoMaisAntigoCrediario
                        WHEN ra.DtVencimentoMaisAntigoCrediario <= ta.DtVencimentoMaisAntigoTerceiros THEN ra.DtVencimentoMaisAntigoCrediario
                        ELSE ta.DtVencimentoMaisAntigoTerceiros
                    END AS DtVencimentoMaisAntigoEmAberto
                ) datas
            )
            """;
    }

    private static string BuildWhere(
        string? busca,
        bool hasCodigoBusca,
        int? codCli,
        int? loja,
        int? codGrupo,
        SexoFiltro sexoFiltro,
        decimal? rendaMin,
        decimal? rendaMax,
        decimal? limiteDispMin,
        decimal? limiteDispMax,
        bool somenteSemParcelasEmAberto,
        bool somenteComParcelasEmAberto,
        bool comTerceiros,
        bool semTerceiros,
        DateTime? quitacaoDe,
        DateTime? quitacaoAteExclusivo)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var buscaFilter = """
                (
                    db.Nome LIKE @Busca
                    OR db.Bairro LIKE @Busca
                    OR db.NomeCidade LIKE @Busca
                    OR db.Fone LIKE @Busca
                    OR db.Fwhats LIKE @Busca
                    OR db.FoneReferencia1 LIKE @Busca
                    OR db.FoneReferencia2 LIKE @Busca
                """;

            if (hasCodigoBusca)
            {
                buscaFilter += " OR db.Codigo = @CodigoBusca";
            }

            buscaFilter += ")";
            filters.Add(buscaFilter);
        }

        if (codCli.HasValue)
        {
            filters.Add("db.Codigo = @CodCli");
        }

        if (loja.HasValue)
        {
            filters.Add("db.Loja = @Loja");
        }

        if (codGrupo.HasValue)
        {
            filters.Add("db.CodGrupo = @CodGrupo");
        }

        if (sexoFiltro.Codigo.HasValue)
        {
            filters.Add("db.Sexo = @SexoDescricao");
        }
        else if (sexoFiltro.FiltrarNaoInformado)
        {
            filters.Add("db.Sexo = 'Nao informado'");
        }

        if (rendaMin.HasValue)
        {
            filters.Add("ISNULL(db.Renda, 0) >= @RendaMin");
        }

        if (rendaMax.HasValue)
        {
            filters.Add("ISNULL(db.Renda, 0) <= @RendaMax");
        }

        if (limiteDispMin.HasValue)
        {
            filters.Add("db.LimiteDisponivel >= @LimiteDispMin");
        }

        if (limiteDispMax.HasValue)
        {
            filters.Add("db.LimiteDisponivel <= @LimiteDispMax");
        }

        if (somenteSemParcelasEmAberto)
        {
            filters.Add("db.QtdParcelasEmAberto = 0");
        }

        if (somenteComParcelasEmAberto)
        {
            filters.Add("db.QtdParcelasEmAberto > 0");
        }

        if (comTerceiros)
        {
            filters.Add("db.QtdEmAbertoTerceiros > 0");
        }

        if (semTerceiros)
        {
            filters.Add("db.QtdEmAbertoTerceiros = 0");
        }

        if (quitacaoDe.HasValue)
        {
            filters.Add("db.DtUltimaQuitacaoCarne >= @QuitacaoDe");
        }

        if (quitacaoAteExclusivo.HasValue)
        {
            filters.Add("db.DtUltimaQuitacaoCarne < @QuitacaoAteExclusivo");
        }

        return filters.Count == 0
            ? string.Empty
            : "WHERE " + string.Join(" AND ", filters);
    }

    private static void AddParameters(
        SqlCommand command,
        string? busca,
        bool hasCodigoBusca,
        int codigoBusca,
        int? codCli,
        int? loja,
        int? codGrupo,
        SexoFiltro sexoFiltro,
        decimal? rendaMin,
        decimal? rendaMax,
        decimal? limiteDispMin,
        decimal? limiteDispMax,
        DateTime? quitacaoDe,
        DateTime? quitacaoAteExclusivo)
    {
        if (!string.IsNullOrWhiteSpace(busca))
        {
            command.Parameters.Add("@Busca", SqlDbType.NVarChar, 200).Value = $"%{busca}%";

            if (hasCodigoBusca)
            {
                command.Parameters.Add("@CodigoBusca", SqlDbType.Int).Value = codigoBusca;
            }
        }

        if (codCli.HasValue)
        {
            command.Parameters.Add("@CodCli", SqlDbType.Int).Value = codCli.Value;
        }

        if (loja.HasValue)
        {
            command.Parameters.Add("@Loja", SqlDbType.Int).Value = loja.Value;
        }

        if (codGrupo.HasValue)
        {
            command.Parameters.Add("@CodGrupo", SqlDbType.Int).Value = codGrupo.Value;
        }

        if (sexoFiltro.Codigo.HasValue)
        {
            command.Parameters.Add("@SexoDescricao", SqlDbType.NVarChar, 20).Value = sexoFiltro.Codigo.Value == 0 ? "Homem" : "Mulher";
        }

        if (rendaMin.HasValue)
        {
            command.Parameters.Add("@RendaMin", SqlDbType.Decimal).Value = rendaMin.Value;
        }

        if (rendaMax.HasValue)
        {
            command.Parameters.Add("@RendaMax", SqlDbType.Decimal).Value = rendaMax.Value;
        }

        if (limiteDispMin.HasValue)
        {
            command.Parameters.Add("@LimiteDispMin", SqlDbType.Decimal).Value = limiteDispMin.Value;
        }

        if (limiteDispMax.HasValue)
        {
            command.Parameters.Add("@LimiteDispMax", SqlDbType.Decimal).Value = limiteDispMax.Value;
        }

        if (quitacaoDe.HasValue)
        {
            command.Parameters.Add("@QuitacaoDe", SqlDbType.DateTime).Value = quitacaoDe.Value;
        }

        if (quitacaoAteExclusivo.HasValue)
        {
            command.Parameters.Add("@QuitacaoAteExclusivo", SqlDbType.DateTime).Value = quitacaoAteExclusivo.Value;
        }
    }

    private static bool TryResolveSexoFilter(string? sexo, out SexoFiltro filtro, out string? error)
    {
        filtro = new SexoFiltro();
        error = null;

        if (string.IsNullOrWhiteSpace(sexo))
        {
            return true;
        }

        var normalized = sexo.Trim().ToLowerInvariant().Replace(" ", string.Empty).Replace("-", string.Empty);

        if (normalized is "0" or "homem" or "masculino")
        {
            filtro.Codigo = 0;
            return true;
        }

        if (normalized is "1" or "mulher" or "feminino")
        {
            filtro.Codigo = 1;
            return true;
        }

        if (normalized is "naoinformado" or "naoinformada" or "nao")
        {
            filtro.FiltrarNaoInformado = true;
            return true;
        }

        error = "O parametro 'sexo' deve ser 0/homem, 1/mulher ou nao-informado.";
        return false;
    }

    private static ClienteCrmIndicadores ReadClienteCrmIndicadores(SqlDataReader reader)
    {
        return new ClienteCrmIndicadores
        {
            Codigo = Convert.ToInt32(reader.GetValue(0)),
            Nome = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Bairro = ReadString(reader, 2),
            NomeCidade = ReadString(reader, 3),
            DtNascimento = ReadDateTime(reader, 4),
            Sexo = reader.IsDBNull(5) ? "Nao informado" : reader.GetString(5),
            CodGrupo = ReadInt32(reader, 6),
            Limite = ReadDecimal(reader, 7),
            Renda = ReadDecimal(reader, 8),
            Idade = ReadInt32(reader, 9),
            Loja = ReadInt32(reader, 10),
            Fone = ReadString(reader, 11),
            Fwhats = ReadString(reader, 12),
            FoneReferencia1 = ReadString(reader, 13),
            FoneReferencia2 = ReadString(reader, 14),
            QtdEmAbertoCrediario = Convert.ToInt32(reader.GetValue(15)),
            TotalEmAbertoCrediario = Convert.ToDecimal(reader.GetValue(16)),
            QtdEmAbertoTerceiros = Convert.ToInt32(reader.GetValue(17)),
            TotalEmAbertoTerceiros = Convert.ToDecimal(reader.GetValue(18)),
            QtdParcelasEmAberto = Convert.ToInt32(reader.GetValue(19)),
            TotalEmAberto = Convert.ToDecimal(reader.GetValue(20)),
            TemParcelasEmAberto = Convert.ToBoolean(reader.GetValue(21)),
            LimiteDisponivel = ReadDecimal(reader, 22),
            DtVencimentoMaisAntigoEmAberto = ReadDateTime(reader, 23),
            DiasMaiorAtraso = ReadInt32(reader, 24),
            DtUltimaBaixa = ReadDateTime(reader, 25),
            DtUltimaQuitacaoCarne = ReadDateTime(reader, 26)
        };
    }

    private static string? ReadString(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal).ToString();
    }

    private static int? ReadInt32(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static decimal? ReadDecimal(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static DateTime? ReadDateTime(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));
    }

    private sealed class SexoFiltro
    {
        public int? Codigo { get; set; }

        public bool FiltrarNaoInformado { get; set; }
    }
}

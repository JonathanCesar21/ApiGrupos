using System.Data;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/recebimentos/crediarista")]
public class RecebimentosCrediaristaController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private const int MaxPeriodoDias = 120;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public RecebimentosCrediaristaController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet("titulos")]
    public async Task<ActionResult> GetTitulos(
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] int? loja,
        [FromQuery] string? tipoData = null,
        [FromQuery] bool somenteEmAberto = true,
        [FromQuery] bool somentePagos = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolvePeriodo(dataInicio, dataFim, tipoData, out var filtros, out var error))
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

            var titulos = new List<RecebimentoCrediaristaTitulo>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"""
                SELECT
                    c.Vencimento AS DtVencimento,
                    c.Valor,
                    c.CodCli,
                    cli.nome AS NomeCliente,
                    cli.Bairro,
                    cid.Cidade AS NomeCidade,
                    cli.dtNascimento,
                    CASE cli.Sexo
                        WHEN 0 THEN 'Homem'
                        WHEN 1 THEN 'Mulher'
                        ELSE 'Nao informado'
                    END AS Sexo,
                    cli.CodGrupo,
                    cli.Limite,
                    cli.Renda,
                    cli.Idade,
                    cli.Loja AS LojaCadastro,
                    cli.Fone,
                    cli.FoneRef1 AS FoneReferencia1,
                    cli.FoneRef2 AS FoneReferencia2,
                    c.Pedido,
                    c.DtPedido,
                    c.Parcela,
                    c.NParcelas,
                    c.Empresa AS Loja,
                    c.Pago,
                    c.Baixa AS DtBaixa,
                    c.FormaPagto AS CodFormaPgt,
                    fp.Forma AS FormaPagamento
                FROM CReceber c
                INNER JOIN FormaPG fp
                    ON c.FormaPagto = fp.codforma
                INNER JOIN Clientes cli
                    ON c.CodCli = cli.Codigo
                INNER JOIN Cidades cid
                    ON cli.Cidade = cid.Seq
                WHERE {filtros.DateColumn} >= @DataInicio
                  AND {filtros.DateColumn} < @DataFimExclusivo
                  AND DATEDIFF(DAY, c.Vencimento, CAST(GETDATE() AS date)) <= 70
                  {GetPagamentoFilter(somenteEmAberto, somentePagos)}
                  {GetLojaFilter(loja)}
                ORDER BY c.Vencimento, c.Empresa, cli.nome, c.Pedido, c.Parcela
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            AddCommonParameters(command, filtros, loja);

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                titulos.Add(ReadTitulo(reader));
            }

            return Ok(titulos);
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
                $"Erro ao consultar recebimentos crediarista no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar recebimentos crediarista: {ex.Message}");
        }
    }

    [HttpGet("clientes-resumo")]
    public async Task<ActionResult> GetClientesResumo(
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] int? loja,
        [FromQuery] string? tipoData = null,
        [FromQuery] bool somenteEmAberto = true,
        [FromQuery] bool somentePagos = false,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolvePeriodo(dataInicio, dataFim, tipoData, out var filtros, out var error))
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

            var clientes = new List<RecebimentoCrediaristaClienteResumo>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var sql = $"""
                SELECT
                    c.CodCli,
                    cli.nome AS NomeCliente,
                    cli.Bairro,
                    cid.Cidade AS NomeCidade,
                    cli.Limite,
                    cli.Renda,
                    cli.Loja AS LojaCadastro,
                    cli.Fone,
                    cli.FoneRef1 AS FoneReferencia1,
                    cli.FoneRef2 AS FoneReferencia2,
                    COUNT(1) AS QtdeTitulos,
                    SUM(c.Valor) AS ValorTotal,
                    cli.Limite - SUM(c.Valor) AS LimiteDisponivel,
                    MIN(c.Vencimento) AS PrimeiroVencimento,
                    MAX(c.Vencimento) AS UltimoVencimento,
                    MAX(c.DtPedido) AS UltimaCompra,
                    CONVERT(varchar(20), MIN(c.Empresa)) AS Lojas
                FROM CReceber c
                INNER JOIN Clientes cli
                    ON c.CodCli = cli.Codigo
                INNER JOIN Cidades cid
                    ON cli.Cidade = cid.Seq
                WHERE {filtros.DateColumn} >= @DataInicio
                  AND {filtros.DateColumn} < @DataFimExclusivo
                  AND DATEDIFF(DAY, c.Vencimento, CAST(GETDATE() AS date)) <= 70
                  {GetPagamentoFilter(somenteEmAberto, somentePagos)}
                  {GetLojaFilter(loja)}
                GROUP BY
                    c.CodCli,
                    cli.nome,
                    cli.Bairro,
                    cid.Cidade,
                    cli.Limite,
                    cli.Renda,
                    cli.Loja,
                    cli.Fone,
                    cli.FoneRef1,
                    cli.FoneRef2
                ORDER BY
                    MIN(c.Vencimento),
                    SUM(c.Valor) DESC,
                    cli.nome
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            AddCommonParameters(command, filtros, loja);

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                clientes.Add(ReadClienteResumo(reader));
            }

            return Ok(clientes);
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
                $"Erro ao consultar resumo de recebimentos crediarista no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar resumo de recebimentos crediarista: {ex.Message}");
        }
    }

    private static bool TryResolvePeriodo(
        DateTime? dataInicio,
        DateTime? dataFim,
        string? tipoData,
        out RecebimentosCrediaristaFiltros filtros,
        out string error)
    {
        filtros = new RecebimentosCrediaristaFiltros();
        error = string.Empty;

        if (!TryResolveDataColumn(tipoData, out var dateColumn, out error))
        {
            return false;
        }

        filtros.DateColumn = dateColumn;

        if (!dataInicio.HasValue || !dataFim.HasValue)
        {
            error = "Os parametros 'dataInicio' e 'dataFim' sao obrigatorios.";
            return false;
        }

        filtros.Inicio = dataInicio.Value.Date;
        var fim = dataFim.Value.Date;

        if (fim < filtros.Inicio)
        {
            error = "O parametro 'dataFim' deve ser maior ou igual a 'dataInicio'.";
            return false;
        }

        if (fim == DateTime.MaxValue.Date)
        {
            error = "O parametro 'dataFim' excede o limite suportado.";
            return false;
        }

        var periodoDias = (fim - filtros.Inicio).TotalDays + 1;
        if (periodoDias > MaxPeriodoDias)
        {
            error = $"O periodo maximo permitido para recebimentos crediarista e de {MaxPeriodoDias} dias.";
            return false;
        }

        filtros.FimExclusivo = fim.AddDays(1);
        return true;
    }

    private static bool TryResolveDataColumn(string? tipoData, out string dateColumn, out string error)
    {
        var key = string.IsNullOrWhiteSpace(tipoData)
            ? "vencimento"
            : tipoData.Trim().ToLowerInvariant();

        dateColumn = key switch
        {
            "vencimento" => "c.Vencimento",
            "baixa" => "c.Baixa",
            "dtbaixa" => "c.Baixa",
            "pagamento" => "c.Baixa",
            "quitacao" => "c.Baixa",
            "pedido" => "c.DtPedido",
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(dateColumn))
        {
            error = string.Empty;
            return true;
        }

        error = "O parametro 'tipoData' deve ser um destes valores: vencimento, baixa, dtbaixa, pagamento, quitacao ou pedido.";
        return false;
    }

    private static string GetPagamentoFilter(bool somenteEmAberto, bool somentePagos)
    {
        if (somentePagos)
        {
            return "AND c.Pago IS NOT NULL";
        }

        return somenteEmAberto ? "AND c.Pago IS NULL" : string.Empty;
    }

    private static string GetLojaFilter(int? loja)
    {
        return loja.HasValue ? "AND c.Empresa = @Loja" : string.Empty;
    }

    private static void AddCommonParameters(SqlCommand command, RecebimentosCrediaristaFiltros filtros, int? loja)
    {
        command.Parameters.Add("@DataInicio", SqlDbType.DateTime).Value = filtros.Inicio;
        command.Parameters.Add("@DataFimExclusivo", SqlDbType.DateTime).Value = filtros.FimExclusivo;

        if (loja.HasValue)
        {
            command.Parameters.Add("@Loja", SqlDbType.Int).Value = loja.Value;
        }
    }

    private static RecebimentoCrediaristaTitulo ReadTitulo(SqlDataReader reader)
    {
        return new RecebimentoCrediaristaTitulo
        {
            DtVencimento = Convert.ToDateTime(reader.GetValue(0)),
            Valor = reader.IsDBNull(1) ? 0 : Convert.ToDecimal(reader.GetValue(1)),
            CodCli = Convert.ToInt32(reader.GetValue(2)),
            NomeCliente = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Bairro = ReadString(reader, 4),
            NomeCidade = ReadString(reader, 5),
            DtNascimento = ReadDateTime(reader, 6),
            Sexo = reader.IsDBNull(7) ? "Nao informado" : reader.GetString(7),
            CodGrupo = ReadInt32(reader, 8),
            Limite = ReadDecimal(reader, 9),
            Renda = ReadDecimal(reader, 10),
            Idade = ReadInt32(reader, 11),
            LojaCadastro = ReadInt32(reader, 12),
            Fone = ReadString(reader, 13),
            FoneReferencia1 = ReadString(reader, 14),
            FoneReferencia2 = ReadString(reader, 15),
            Pedido = ReadString(reader, 16),
            DtPedido = ReadDateTime(reader, 17),
            Parcela = ReadInt32(reader, 18),
            NParcelas = ReadInt32(reader, 19),
            Loja = ReadInt32(reader, 20),
            Pago = ReadBoolean(reader, 21),
            DtBaixa = ReadDateTime(reader, 22),
            CodFormaPgt = ReadInt32(reader, 23),
            FormaPagamento = ReadString(reader, 24)
        };
    }

    private static RecebimentoCrediaristaClienteResumo ReadClienteResumo(SqlDataReader reader)
    {
        return new RecebimentoCrediaristaClienteResumo
        {
            CodCli = Convert.ToInt32(reader.GetValue(0)),
            NomeCliente = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Bairro = ReadString(reader, 2),
            NomeCidade = ReadString(reader, 3),
            Limite = ReadDecimal(reader, 4),
            Renda = ReadDecimal(reader, 5),
            LojaCadastro = ReadInt32(reader, 6),
            Fone = ReadString(reader, 7),
            FoneReferencia1 = ReadString(reader, 8),
            FoneReferencia2 = ReadString(reader, 9),
            QtdeTitulos = Convert.ToInt32(reader.GetValue(10)),
            ValorTotal = reader.IsDBNull(11) ? 0 : Convert.ToDecimal(reader.GetValue(11)),
            LimiteDisponivel = ReadDecimal(reader, 12),
            PrimeiroVencimento = Convert.ToDateTime(reader.GetValue(13)),
            UltimoVencimento = Convert.ToDateTime(reader.GetValue(14)),
            UltimaCompra = ReadDateTime(reader, 15),
            Lojas = reader.IsDBNull(16) ? string.Empty : reader.GetString(16)
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

    private static bool? ReadBoolean(SqlDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            bool booleanValue => booleanValue,
            byte byteValue => byteValue != 0,
            short shortValue => shortValue != 0,
            int intValue => intValue != 0,
            long longValue => longValue != 0,
            string textValue when bool.TryParse(textValue, out var parsed) => parsed,
            string textValue when int.TryParse(textValue, out var parsed) => parsed != 0,
            _ => Convert.ToBoolean(value)
        };
    }

    private static DateTime? ReadDateTime(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));
    }

    private sealed class RecebimentosCrediaristaFiltros
    {
        public DateTime Inicio { get; set; }

        public DateTime FimExclusivo { get; set; }

        public string DateColumn { get; set; } = string.Empty;
    }
}

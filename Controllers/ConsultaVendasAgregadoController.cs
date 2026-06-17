using System.Data;
using System.Globalization;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/consulta-vendas-agregado")]
public class ConsultaVendasAgregadoController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private static readonly DateTime MinDataInicio = new(2024, 1, 1);
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ConsultaVendasAgregadoController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get(
        [FromQuery] string? dataInicio,
        [FromQuery] string? dataFim,
        CancellationToken cancellationToken)
    {
        if (!TryResolveDateRange(dataInicio, dataFim, out var inicio, out var fimExclusivo, out var error))
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

            var itens = new List<ConsultaVendaAgregadaItem>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT
                    CONVERT(date, data) AS data_venda,
                    ISNULL(referencia, '') AS referencia,
                    ISNULL(CAST(Numero AS varchar(50)), '') AS numero,
                    ISNULL(CAST(loja AS varchar(20)), '') AS loja,
                    ISNULL(Grupo, '') AS grupo,
                    ISNULL(SubGrupo, '') AS subgrupo,
                    ISNULL(Fornecedor, '') AS fornecedor,
                    SUM(ISNULL(quant, 0)) AS qtde_venda,
                    SUM(
                        CASE
                            WHEN ISNULL(quant, 0) < 0 AND ISNULL(ValorReal, 0) > 0 THEN -ISNULL(ValorReal, 0)
                            WHEN ISNULL(quant, 0) > 0 AND ISNULL(ValorReal, 0) < 0 THEN -ISNULL(ValorReal, 0)
                            ELSE ISNULL(ValorReal, 0)
                        END
                    ) AS total_venda,
                    SUM(ISNULL(ValorCusto, 0)) AS total_custo
                FROM ConsultaVenda
                WHERE data >= @DataInicio
                  AND data < @DataFimExclusivo
                GROUP BY
                    CONVERT(date, data),
                    ISNULL(referencia, ''),
                    ISNULL(CAST(Numero AS varchar(50)), ''),
                    ISNULL(CAST(loja AS varchar(20)), ''),
                    ISNULL(Grupo, ''),
                    ISNULL(SubGrupo, ''),
                    ISNULL(Fornecedor, '')
                HAVING SUM(ISNULL(quant, 0)) <> 0
                ORDER BY data_venda DESC
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            command.Parameters.Add("@DataInicio", SqlDbType.DateTime).Value = inicio;
            command.Parameters.Add("@DataFimExclusivo", SqlDbType.DateTime).Value = fimExclusivo;

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                itens.Add(new ConsultaVendaAgregadaItem
                {
                    DataVenda = DateOnly.FromDateTime(reader.GetDateTime(0)),
                    Referencia = reader.GetString(1),
                    Numero = reader.GetString(2),
                    Loja = reader.GetString(3),
                    Grupo = reader.GetString(4),
                    SubGrupo = reader.GetString(5),
                    Fornecedor = reader.GetString(6),
                    QtdeVenda = Convert.ToDecimal(reader.GetValue(7), CultureInfo.InvariantCulture),
                    TotalVenda = Convert.ToDecimal(reader.GetValue(8), CultureInfo.InvariantCulture),
                    TotalCusto = Convert.ToDecimal(reader.GetValue(9), CultureInfo.InvariantCulture)
                });
            }

            return Ok(itens);
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
                $"Erro ao consultar vendas agregadas no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar vendas agregadas: {ex.Message}");
        }
    }

    private static bool TryResolveDateRange(
        string? dataInicio,
        string? dataFim,
        out DateTime inicio,
        out DateTime fimExclusivo,
        out string? error)
    {
        inicio = default;
        fimExclusivo = default;
        error = null;

        if (string.IsNullOrWhiteSpace(dataInicio) || string.IsNullOrWhiteSpace(dataFim))
        {
            error = "Os parametros 'dataInicio' e 'dataFim' sao obrigatorios no formato YYYY-MM-DD.";
            return false;
        }

        if (!DateTime.TryParseExact(dataInicio, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out inicio) ||
            !DateTime.TryParseExact(dataFim, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fim))
        {
            error = "Os parametros 'dataInicio' e 'dataFim' devem estar no formato YYYY-MM-DD.";
            return false;
        }

        if (fim < inicio)
        {
            error = "O parametro 'dataFim' deve ser maior ou igual a 'dataInicio'.";
            return false;
        }

        if (inicio < MinDataInicio)
        {
            error = $"O parametro 'dataInicio' deve ser maior ou igual a {MinDataInicio:yyyy-MM-dd}.";
            return false;
        }

        if (fim == DateTime.MaxValue.Date)
        {
            error = "O parametro 'dataFim' excede o limite suportado.";
            return false;
        }

        fimExclusivo = fim.AddDays(1);
        return true;
    }
}

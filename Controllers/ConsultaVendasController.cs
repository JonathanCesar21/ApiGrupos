using System.Data;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/consulta-vendas")]
public class ConsultaVendasController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ConsultaVendasController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get(
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        CancellationToken cancellationToken)
    {
        if (!dataInicio.HasValue || !dataFim.HasValue)
        {
            return BadRequest("Os parametros 'dataInicio' e 'dataFim' sao obrigatorios.");
        }

        var inicio = dataInicio.Value.Date;
        var fim = dataFim.Value.Date;

        if (fim < inicio)
        {
            return BadRequest("O parametro 'dataFim' deve ser maior ou igual a 'dataInicio'.");
        }

        if (fim == DateTime.MaxValue.Date)
        {
            return BadRequest("O parametro 'dataFim' excede o limite suportado.");
        }

        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var fimExclusivo = fim.AddDays(1);
            var vendas = new List<Dictionary<string, object?>>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT
                    codcli,
                    quant,
                    Categoria,
                    pedido,
                    valor,
                    ValorTotal,
                    DescUnitario,
                    ValorReal,
                    DescontoVenda,
                    data,
                    Cor,
                    Numero,
                    vendedor,
                    subtotal,
                    total,
                    desconto,
                    Secao,
                    referencia,
                    Fornecedor,
                    Grupo,
                    SubGrupo,
                    loja,
                    ValorCusto,
                    colecao,
                    DtProduto,
                    Departamento
                FROM ConsultaVenda
                WHERE data >= @DataInicio
                  AND data < @DataFimExclusivo
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            command.Parameters.Add("@DataInicio", SqlDbType.DateTime2).Value = inicio;
            command.Parameters.Add("@DataFimExclusivo", SqlDbType.DateTime2).Value = fimExclusivo;

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var venda = new Dictionary<string, object?>(reader.FieldCount);

                for (var columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
                {
                    venda[reader.GetName(columnIndex)] = await reader.IsDBNullAsync(columnIndex, cancellationToken)
                        ? null
                        : reader.GetValue(columnIndex);
                }

                vendas.Add(venda);
            }

            return Ok(vendas);
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
                $"Erro ao consultar vendas no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar vendas: {ex.Message}");
        }
    }
}

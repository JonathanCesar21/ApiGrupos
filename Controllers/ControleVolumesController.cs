using System.Data;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ControleVolumesController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ControleVolumesController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var volumes = new List<Dictionary<string, object?>>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT
                    Lote,
                    Descricao,
                    quant,
                    total,
                    data,
                    usuario,
                    Loja,
                    Status
                FROM ControleVolumes
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var volume = new Dictionary<string, object?>(reader.FieldCount);

                for (var columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
                {
                    volume[reader.GetName(columnIndex)] = await reader.IsDBNullAsync(columnIndex, cancellationToken)
                        ? null
                        : reader.GetValue(columnIndex);
                }

                volumes.Add(volume);
            }

            return Ok(volumes);
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
                $"Erro ao consultar controle de volumes no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar controle de volumes: {ex.Message}");
        }
    }
}

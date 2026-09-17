using System.Data;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/transferencias-lojas-pendentes")]
public class TransferenciasLojasPendentesController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public TransferenciasLojasPendentesController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get(
        [FromQuery] string? notaFiscal,
        CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var notaFiscalNormalizada = string.IsNullOrWhiteSpace(notaFiscal) ? null : notaFiscal.Trim();
            var transferencias = new List<Dictionary<string, object?>>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Seq, Codigo e Data mudam na efetivacao; o vinculo deve usar a nota e o item.
            var sql = """
                SELECT
                    tc.Codprod,
                    tc.CodCor,
                    tc.CodFor,
                    tc.Referencia,
                    tc.Origem,
                    tc.Destino,
                    tc.Quant,
                    tc.Seq,
                    tc.CodNumero,
                    tc.NotaFiscal,
                    tc.Etiqueta,
                    tc.Impresso,
                    tc.Data,
                    tc.Codigo,
                    tc.Agrupar,
                    tc.Faturado,
                    tc.SerieNota,
                    tc.LojaNota,
                    tc.Obs,
                    tc.Usuario,
                    tc.IdEmpresa,
                    tc.IdFilial,
                    tc.AtualizadoMatriz
                FROM TransferenciaControle tc
                WHERE NOT EXISTS
                (
                    SELECT 1
                    FROM Transferencia t
                    WHERE t.NotaFiscal = tc.NotaFiscal
                      AND t.Origem = tc.Origem
                      AND t.Destino = tc.Destino
                      AND t.Codprod = tc.Codprod
                      AND (t.CodCor = tc.CodCor OR (t.CodCor IS NULL AND tc.CodCor IS NULL))
                      AND (t.CodNumero = tc.CodNumero OR (t.CodNumero IS NULL AND tc.CodNumero IS NULL))
                      AND (t.IdEmpresa = tc.IdEmpresa OR (t.IdEmpresa IS NULL AND tc.IdEmpresa IS NULL))
                      AND (t.IdFilial = tc.IdFilial OR (t.IdFilial IS NULL AND tc.IdFilial IS NULL))
                )
                """;

            if (notaFiscalNormalizada is not null)
            {
                sql += "\nAND tc.NotaFiscal = @NotaFiscal";
            }

            sql += "\nORDER BY tc.Data DESC, tc.NotaFiscal, tc.Origem, tc.Destino, tc.Seq";

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            if (notaFiscalNormalizada is not null)
            {
                command.Parameters.Add("@NotaFiscal", SqlDbType.VarChar).Value = notaFiscalNormalizada;
            }

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var transferencia = new Dictionary<string, object?>(reader.FieldCount);

                for (var columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
                {
                    transferencia[reader.GetName(columnIndex)] = await reader.IsDBNullAsync(columnIndex, cancellationToken)
                        ? null
                        : reader.GetValue(columnIndex);
                }

                transferencias.Add(transferencia);
            }

            return Ok(transferencias);
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
                $"Erro ao consultar transferencias pendentes entre lojas no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar transferencias pendentes entre lojas: {ex.Message}");
        }
    }
}

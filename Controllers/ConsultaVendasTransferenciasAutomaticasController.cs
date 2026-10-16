using System.Data;
using System.Globalization;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/consulta-vendas-transferencias-automaticas")]
public class ConsultaVendasTransferenciasAutomaticasController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private const int PedidoMaxLength = 100;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ConsultaVendasTransferenciasAutomaticasController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    [ProducesResponseType(typeof(VendaTransferenciasAutomaticasResposta), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public async Task<ActionResult<VendaTransferenciasAutomaticasResposta>> Get(
        [FromQuery] int? loja,
        [FromQuery] string? pedido,
        CancellationToken cancellationToken)
    {
        if (!loja.HasValue || loja.Value <= 0)
        {
            return BadRequest("O parametro 'loja' e obrigatorio e deve ser um inteiro positivo.");
        }

        var pedidoNormalizado = pedido?.Trim();
        if (string.IsNullOrEmpty(pedidoNormalizado) || pedidoNormalizado.Length > PedidoMaxLength)
        {
            return BadRequest($"O parametro 'pedido' e obrigatorio e deve ter ate {PedidoMaxLength} caracteres.");
        }

        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // A consulta e limitada a uma venda. Impressao e DataAuto nao filtram
            // o vinculo, para incluir transferencias pendentes e ja expedidas.
            const string sql = """
                SELECT
                    v.referencia,
                    v.Cor,
                    v.Numero,
                    produto.barras,
                    v.quant,
                    atendimento.TransferenciaAutomatica
                FROM ConsultaVenda v
                CROSS APPLY
                (
                    SELECT CASE WHEN EXISTS
                    (
                        SELECT 1
                        FROM Transferencia t
                        LEFT JOIN Cor c ON c.Codigo = t.CodCor
                        LEFT JOIN Numeracao n ON n.Codigo = t.CodNumero
                        WHERE t.Automatica = '1'
                          AND TRY_CONVERT(int, t.Origem) = 5
                          AND TRY_CONVERT(int, t.Destino) = @Loja
                          AND t.NotaFiscal = v.pedido
                          AND t.referencia = v.referencia
                          AND (c.Descricao = v.Cor OR (t.CodCor IS NULL AND v.Cor IS NULL))
                          AND (n.Descricao = v.Numero OR (t.CodNumero IS NULL AND v.Numero IS NULL))
                    ) THEN 1 ELSE 0 END AS TransferenciaAutomatica
                ) atendimento
                CROSS APPLY
                (
                    SELECT CASE
                        WHEN COUNT(DISTINCT pb.barras) = 1 THEN MIN(pb.barras)
                        ELSE NULL
                    END AS barras
                    FROM ProdutoBarras pb
                    WHERE pb.referencia = v.referencia
                      AND (pb.Cor = v.Cor OR (pb.Cor IS NULL AND v.Cor IS NULL))
                      AND (pb.Numero = v.Numero OR (pb.Numero IS NULL AND v.Numero IS NULL))
                      AND NULLIF(LTRIM(RTRIM(pb.barras)), '') IS NOT NULL
                ) produto
                WHERE v.pedido = @Pedido
                  AND TRY_CONVERT(int, v.loja) = @Loja
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };
            command.Parameters.Add("@Loja", SqlDbType.Int).Value = loja.Value;
            command.Parameters.Add("@Pedido", SqlDbType.VarChar, PedidoMaxLength).Value = pedidoNormalizado;

            var resposta = new VendaTransferenciasAutomaticasResposta
            {
                Loja = loja.Value.ToString("D2", CultureInfo.InvariantCulture),
                Pedido = pedidoNormalizado
            };

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                resposta.Itens.Add(new VendaTransferenciaAutomaticaItem
                {
                    Referencia = reader.IsDBNull(0) ? null : reader.GetValue(0).ToString(),
                    Cor = reader.IsDBNull(1) ? null : reader.GetValue(1).ToString(),
                    Numero = reader.IsDBNull(2) ? null : reader.GetValue(2).ToString(),
                    Barras = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Quant = reader.IsDBNull(4) ? null : Convert.ToDecimal(reader.GetValue(4), CultureInfo.InvariantCulture),
                    TransferenciaAutomatica = reader.GetInt32(5)
                });
            }

            if (resposta.Itens.Count == 0)
            {
                return NotFound("Venda nao encontrada para a loja e o pedido informados.");
            }

            return Ok(resposta);
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
                $"Erro ao consultar transferencias automaticas da venda no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar transferencias automaticas da venda: {ex.Message}");
        }
    }
}

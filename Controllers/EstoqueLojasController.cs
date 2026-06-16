using System.Data;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/estoque-lojas")]
public class EstoqueLojasController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public EstoqueLojasController(ConnectionStringProvider connectionStringProvider)
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

            var itens = new List<EstoqueLojaItem>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT
                    ip.codprod,
                    ip.barras,
                    pb.Grupo,
                    pb.SubGrupo,
                    pb.CodNumero,
                    pb.numero,
                    pb.Referencia,
                    pb.Fornecedor,
                    pb.ValorCusto,
                    pb.valor,
                    pb.Colecao,
                    estoque.quant,
                    estoque.loja,
                    p.categoria AS Categoria,
                    ultimaEntrada.data AS UltimaDataEntrada
                FROM itemProduto ip
                INNER JOIN produtoBarras pb
                    ON ip.barras = pb.barras
                INNER JOIN produtos p
                    ON ip.codprod = p.codigo
                CROSS APPLY
                (
                    VALUES
                        ('1', ip.loja1),
                        ('2', ip.loja2),
                        ('3', ip.loja3),
                        ('4', ip.loja4),
                        ('5', ip.loja5),
                        ('7', ip.loja7),
                        ('8', ip.loja8),
                        ('9', ip.loja9),
                        ('10', ip.loja10),
                        ('11', ip.loja11),
                        ('12', ip.loja12),
                        ('13', ip.loja13),
                        ('15', ip.loja15),
                        ('16', ip.loja16),
                        ('17', ip.loja17),
                        ('18', ip.loja18),
                        ('19', ip.loja19),
                        ('20', ip.loja20)
                ) estoque(loja, quant)
                OUTER APPLY
                (
                    SELECT TOP (1)
                        entrada.data
                    FROM Entrada entrada
                    WHERE entrada.codprod = ip.codprod
                      AND entrada.loja = estoque.loja
                      AND entrada.CodNumero = ip.CodNumero
                      AND entrada.CodCor = ip.codcor
                    ORDER BY entrada.data DESC
                ) ultimaEntrada
                WHERE estoque.quant <> 0
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
                itens.Add(new EstoqueLojaItem
                {
                    CodProd = Convert.ToInt32(reader.GetValue(0)),
                    Barras = reader.IsDBNull(1) ? null : reader.GetValue(1).ToString(),
                    Grupo = reader.IsDBNull(2) ? null : reader.GetValue(2).ToString(),
                    SubGrupo = reader.IsDBNull(3) ? null : reader.GetValue(3).ToString(),
                    CodNumero = reader.IsDBNull(4) ? null : reader.GetValue(4).ToString(),
                    Numero = reader.IsDBNull(5) ? null : reader.GetValue(5).ToString(),
                    Referencia = reader.IsDBNull(6) ? null : reader.GetValue(6).ToString(),
                    Fornecedor = reader.IsDBNull(7) ? null : reader.GetValue(7).ToString(),
                    ValorCusto = reader.IsDBNull(8) ? 0 : Convert.ToDecimal(reader.GetValue(8)),
                    Valor = reader.IsDBNull(9) ? 0 : Convert.ToDecimal(reader.GetValue(9)),
                    Colecao = reader.IsDBNull(10) ? null : reader.GetValue(10).ToString(),
                    Quant = reader.IsDBNull(11) ? 0 : Convert.ToInt32(reader.GetValue(11)),
                    Loja = reader.IsDBNull(12) ? null : reader.GetValue(12).ToString(),
                    Categoria = reader.IsDBNull(13) ? null : reader.GetValue(13).ToString(),
                    UltimaDataEntrada = reader.IsDBNull(14) ? null : Convert.ToDateTime(reader.GetValue(14))
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
                $"Erro ao consultar estoque das lojas no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar estoque das lojas: {ex.Message}");
        }
    }
}

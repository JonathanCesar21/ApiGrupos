using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/estoque-matriz")]
public class EstoqueMatrizController : ControllerBase
{
    private readonly ConnectionStringProvider _connectionStringProvider;

    public EstoqueMatrizController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get()
    {
        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var itens = new List<EstoqueMatrizItem>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            const string sql = @"
                SELECT 
                    ip.codprod,
                    ip.codcor,
                    pb.Cor,
                    pb.DescProd,
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
                    loja5 AS quant,
                    '5' AS loja,
                    p.categoria as Categoria,
                    (
                        SELECT TOP 1 data 
                        FROM Entrada 
                        WHERE codprod = ip.codprod AND loja = '5' AND CodNumero = ip.CodNumero AND CodCor = ip.codcor
                        ORDER BY data DESC
                    ) AS UltimaDataEntrada
                FROM itemProduto ip
                JOIN produtoBarras pb 
                    ON ip.barras = pb.barras
                JOIN produtos p
                    ON ip.codprod = p.codigo
                WHERE Loja5 > 0";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                itens.Add(new EstoqueMatrizItem
                {
                    CodProd = Convert.ToInt32(reader.GetValue(0)),
                    CodCor = reader.IsDBNull(1) ? null : Convert.ToInt32(reader.GetValue(1)),
                    Cor = reader.IsDBNull(2) ? null : reader.GetValue(2).ToString(),
                    DescProd = reader.IsDBNull(3) ? null : reader.GetValue(3).ToString(),
                    Barras = reader.IsDBNull(4) ? null : reader.GetValue(4).ToString(),
                    Grupo = reader.IsDBNull(5) ? null : reader.GetValue(5).ToString(),
                    SubGrupo = reader.IsDBNull(6) ? null : reader.GetValue(6).ToString(),
                    CodNumero = reader.IsDBNull(7) ? null : reader.GetValue(7).ToString(),
                    Numero = reader.IsDBNull(8) ? null : reader.GetValue(8).ToString(),
                    Referencia = reader.IsDBNull(9) ? null : reader.GetValue(9).ToString(),
                    Fornecedor = reader.IsDBNull(10) ? null : reader.GetValue(10).ToString(),
                    ValorCusto = reader.IsDBNull(11) ? 0 : reader.GetDecimal(11),
                    Valor = reader.IsDBNull(12) ? 0 : reader.GetDecimal(12),
                    Colecao = reader.IsDBNull(13) ? null : reader.GetValue(13).ToString(),
                    Quant = Convert.ToInt32(reader.GetValue(14)),
                    Loja = reader.GetValue(15).ToString(),
                    Categoria = reader.IsDBNull(16) ? null : reader.GetValue(16).ToString(),
                    UltimaDataEntrada = reader.IsDBNull(17) ? (DateTime?)null : reader.GetDateTime(17)
                });
            }

            return Ok(itens);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Erro interno: {ex.Message}");
        }
    }
}
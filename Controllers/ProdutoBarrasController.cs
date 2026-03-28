using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/produto-barras")]
public class ProdutoBarrasController : ControllerBase
{
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ProdutoBarrasController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] PaginacaoQuery paginacao)
    {
        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var produtoBarras = new List<ProdutoBarra>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            if (!paginacao.HasPagination)
            {
                const string sqlSemPaginacao = @"
                    SELECT p.codigo, pb.referencia, pb.barras, pb.SubGrupo, pb.Grupo, pb.DescProd, pb.Numero, pb.Cor, pb.fornecedor AS NomeFornecedor, pb.CodFor AS CodFornecedor
                    FROM ProdutoBarras pb
                    INNER JOIN Produtos p ON p.referencia = pb.referencia
                    WHERE p.cadastro >= @CadastroMinimo
                    ORDER BY pb.barras";

                await using var commandSemPaginacao = new SqlCommand(sqlSemPaginacao, connection);
                commandSemPaginacao.Parameters.AddWithValue("@CadastroMinimo", new DateTime(2026, 3, 25));
                await using var readerSemPaginacao = await commandSemPaginacao.ExecuteReaderAsync();

                while (await readerSemPaginacao.ReadAsync())
                {
                    produtoBarras.Add(new ProdutoBarra
                    {
                        CodProd = readerSemPaginacao.IsDBNull(0) ? null : Convert.ToInt32(readerSemPaginacao.GetValue(0)),
                        Referencia = readerSemPaginacao.IsDBNull(1) ? string.Empty : readerSemPaginacao.GetString(1),
                        Barras = readerSemPaginacao.IsDBNull(2) ? string.Empty : readerSemPaginacao.GetString(2),
                        SubGrupo = readerSemPaginacao.IsDBNull(3) ? string.Empty : readerSemPaginacao.GetString(3),
                        Grupo = readerSemPaginacao.IsDBNull(4) ? string.Empty : readerSemPaginacao.GetString(4),
                        DescProd = readerSemPaginacao.IsDBNull(5) ? string.Empty : readerSemPaginacao.GetString(5),
                        Numero = readerSemPaginacao.IsDBNull(6) ? string.Empty : readerSemPaginacao.GetValue(6).ToString() ?? string.Empty,
                        Cor = readerSemPaginacao.IsDBNull(7) ? string.Empty : readerSemPaginacao.GetString(7),
                        NomeFornecedor = readerSemPaginacao.IsDBNull(8) ? string.Empty : readerSemPaginacao.GetString(8),
                        CodFornecedor = readerSemPaginacao.IsDBNull(9) ? null : Convert.ToInt32(readerSemPaginacao.GetValue(9))
                    });
                }

                return Ok(produtoBarras);
            }

            if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
            {
                return BadRequest(error);
            }

            const string sqlTotal = @"
                SELECT COUNT(1)
                FROM ProdutoBarras pb
                INNER JOIN Produtos p ON p.referencia = pb.referencia
                WHERE p.cadastro >= @CadastroMinimo";

            await using var commandTotal = new SqlCommand(sqlTotal, connection);
            commandTotal.Parameters.AddWithValue("@CadastroMinimo", new DateTime(2026, 3, 25));
            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            const string sqlPaginado = @"
                WITH Dados AS
                (
                    SELECT
                        pb.referencia,
                        pb.barras,
                        pb.SubGrupo,
                        pb.Grupo,
                        pb.DescProd,
                        pb.Numero,
                        pb.Cor,
                        pb.fornecedor AS NomeFornecedor,
                        pb.CodFor AS CodFornecedor,
                        p.codigo,
                        ROW_NUMBER() OVER (ORDER BY pb.barras) AS RowNum
                    FROM ProdutoBarras pb
                    INNER JOIN Produtos p ON p.referencia = pb.referencia
                    WHERE p.cadastro >= @CadastroMinimo
                )
                SELECT codigo, referencia, barras, SubGrupo, Grupo, DescProd, Numero, Cor, NomeFornecedor, CodFornecedor
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum";

            await using var commandPaginado = new SqlCommand(sqlPaginado, connection);
            commandPaginado.Parameters.AddWithValue("@CadastroMinimo", new DateTime(2026, 3, 25));
            commandPaginado.Parameters.AddWithValue("@RowStart", rowStart);
            commandPaginado.Parameters.AddWithValue("@RowEnd", rowEnd);
            await using var readerPaginado = await commandPaginado.ExecuteReaderAsync();

            while (await readerPaginado.ReadAsync())
            {
                produtoBarras.Add(new ProdutoBarra
                {
                    CodProd = readerPaginado.IsDBNull(0) ? null : Convert.ToInt32(readerPaginado.GetValue(0)),
                    Referencia = readerPaginado.IsDBNull(1) ? string.Empty : readerPaginado.GetString(1),
                    Barras = readerPaginado.IsDBNull(2) ? string.Empty : readerPaginado.GetString(2),
                    SubGrupo = readerPaginado.IsDBNull(3) ? string.Empty : readerPaginado.GetString(3),
                    Grupo = readerPaginado.IsDBNull(4) ? string.Empty : readerPaginado.GetString(4),
                    DescProd = readerPaginado.IsDBNull(5) ? string.Empty : readerPaginado.GetString(5),
                    Numero = readerPaginado.IsDBNull(6) ? string.Empty : readerPaginado.GetValue(6).ToString() ?? string.Empty,
                    Cor = readerPaginado.IsDBNull(7) ? string.Empty : readerPaginado.GetString(7),
                    NomeFornecedor = readerPaginado.IsDBNull(8) ? string.Empty : readerPaginado.GetString(8),
                    CodFornecedor = readerPaginado.IsDBNull(9) ? null : Convert.ToInt32(readerPaginado.GetValue(9))
                });
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<ProdutoBarra>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = produtoBarras
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar produto barras no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar produto barras: {ex.Message}");
        }
    }
}

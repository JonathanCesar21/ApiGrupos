using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

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
    public async Task<ActionResult> Get([FromQuery] PaginacaoQuery paginacao, [FromQuery] string? referencias = null)
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
            var cadastroMinimo = new DateTime(2025, 1, 1);
            var referenciasFiltro = ParseReferencias(referencias);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            if (referenciasFiltro.Count > 0)
            {
                var parametrosReferencia = referenciasFiltro
                    .Select((_, index) => $"@Referencia{index}")
                    .ToArray();

                var sqlPorReferencias = $@"
                    SELECT p.codigo, pb.referencia, pb.barras, pb.SubGrupo, pb.Grupo, pb.DescProd, pb.Numero, pb.Cor, pb.fornecedor AS NomeFornecedor, pb.CodFor AS CodFornecedor, pb.CodSecao, p.Classificacao AS CodClassificacao, p.Categoria AS CodCategoria, p.Colecao AS Colecao, pb.ValorCusto, pb.valor AS Valor
                    FROM ProdutoBarras pb
                    INNER JOIN Produtos p ON p.referencia = pb.referencia
                    WHERE UPPER(LTRIM(RTRIM(pb.referencia))) IN ({string.Join(", ", parametrosReferencia)})
                    ORDER BY pb.barras";

                await using var commandPorReferencias = new SqlCommand(sqlPorReferencias, connection);

                for (var index = 0; index < referenciasFiltro.Count; index++)
                {
                    commandPorReferencias.Parameters.Add(
                        parametrosReferencia[index],
                        SqlDbType.NVarChar,
                        100).Value = referenciasFiltro[index].ToUpperInvariant();
                }

                await using var readerPorReferencias = await commandPorReferencias.ExecuteReaderAsync();

                while (await readerPorReferencias.ReadAsync())
                {
                    produtoBarras.Add(ReadProdutoBarra(readerPorReferencias));
                }

                return Ok(produtoBarras);
            }

            if (!paginacao.HasPagination)
            {
                const string sqlSemPaginacao = @"
                    SELECT p.codigo, pb.referencia, pb.barras, pb.SubGrupo, pb.Grupo, pb.DescProd, pb.Numero, pb.Cor, pb.fornecedor AS NomeFornecedor, pb.CodFor AS CodFornecedor, pb.CodSecao, p.Classificacao AS CodClassificacao, p.Categoria AS CodCategoria, p.Colecao AS Colecao, pb.ValorCusto, pb.valor AS Valor
                    FROM ProdutoBarras pb
                    INNER JOIN Produtos p ON p.referencia = pb.referencia
                    WHERE p.cadastro >= @CadastroMinimo
                    ORDER BY pb.barras";

                await using var commandSemPaginacao = new SqlCommand(sqlSemPaginacao, connection);
                commandSemPaginacao.Parameters.AddWithValue("@CadastroMinimo", cadastroMinimo);
                await using var readerSemPaginacao = await commandSemPaginacao.ExecuteReaderAsync();

                while (await readerSemPaginacao.ReadAsync())
                {
                    produtoBarras.Add(ReadProdutoBarra(readerSemPaginacao));
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
            commandTotal.Parameters.AddWithValue("@CadastroMinimo", cadastroMinimo);
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
                        pb.CodSecao,
                        p.Classificacao AS CodClassificacao,
                        p.Categoria AS CodCategoria,
                        p.Colecao AS Colecao,
                        pb.ValorCusto,
                        pb.valor AS Valor,
                        ROW_NUMBER() OVER (ORDER BY pb.barras) AS RowNum
                    FROM ProdutoBarras pb
                    INNER JOIN Produtos p ON p.referencia = pb.referencia
                    WHERE p.cadastro >= @CadastroMinimo
                )
                SELECT codigo, referencia, barras, SubGrupo, Grupo, DescProd, Numero, Cor, NomeFornecedor, CodFornecedor, CodSecao, CodClassificacao, CodCategoria, Colecao, ValorCusto, Valor
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum";

            await using var commandPaginado = new SqlCommand(sqlPaginado, connection);
            commandPaginado.Parameters.AddWithValue("@CadastroMinimo", cadastroMinimo);
            commandPaginado.Parameters.AddWithValue("@RowStart", rowStart);
            commandPaginado.Parameters.AddWithValue("@RowEnd", rowEnd);
            await using var readerPaginado = await commandPaginado.ExecuteReaderAsync();

            while (await readerPaginado.ReadAsync())
            {
                produtoBarras.Add(ReadProdutoBarra(readerPaginado));
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

    private static List<string> ParseReferencias(string? referencias)
    {
        if (string.IsNullOrWhiteSpace(referencias))
        {
            return new List<string>();
        }

        return referencias
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(referencia => !string.IsNullOrWhiteSpace(referencia))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static ProdutoBarra ReadProdutoBarra(SqlDataReader reader)
    {
        return new ProdutoBarra
        {
            CodProd = reader.IsDBNull(0) ? null : Convert.ToInt32(reader.GetValue(0)),
            Referencia = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Barras = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
            SubGrupo = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
            Grupo = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
            DescProd = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
            Numero = reader.IsDBNull(6) ? string.Empty : reader.GetValue(6).ToString() ?? string.Empty,
            Cor = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
            NomeFornecedor = reader.IsDBNull(8) ? string.Empty : reader.GetString(8),
            CodFornecedor = reader.IsDBNull(9) ? null : Convert.ToInt32(reader.GetValue(9)),
            CodSecao = reader.IsDBNull(10) ? null : Convert.ToInt32(reader.GetValue(10)),
            CodClassificacao = reader.IsDBNull(11) ? null : Convert.ToInt32(reader.GetValue(11)),
            CodCategoria = reader.IsDBNull(12) ? null : Convert.ToInt32(reader.GetValue(12)),
            Colecao = reader.IsDBNull(13) ? string.Empty : reader.GetValue(13).ToString() ?? string.Empty,
            ValorCusto = reader.IsDBNull(14) ? 0 : reader.GetDecimal(14),
            Valor = reader.IsDBNull(15) ? 0 : reader.GetDecimal(15)
        };
    }
}

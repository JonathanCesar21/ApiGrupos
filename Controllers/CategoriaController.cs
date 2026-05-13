using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/categoria")]
public class CategoriaController : ControllerBase
{
    private readonly ConnectionStringProvider _connectionStringProvider;

    public CategoriaController(ConnectionStringProvider connectionStringProvider)
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

            var categorias = new List<Categoria>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            if (!paginacao.HasPagination)
            {
                const string sqlSemPaginacao = @"
                    SELECT codigo AS CodCategoria, nome AS NomeCategoria
                    FROM Categoria
                    ORDER BY nome";

                await using var commandSemPaginacao = new SqlCommand(sqlSemPaginacao, connection);
                await using var readerSemPaginacao = await commandSemPaginacao.ExecuteReaderAsync();

                while (await readerSemPaginacao.ReadAsync())
                {
                    categorias.Add(new Categoria
                    {
                        CodCategoria = Convert.ToInt32(readerSemPaginacao.GetValue(0)),
                        NomeCategoria = readerSemPaginacao.IsDBNull(1) ? string.Empty : readerSemPaginacao.GetString(1)
                    });
                }

                return Ok(categorias);
            }

            if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
            {
                return BadRequest(error);
            }

            const string sqlTotal = "SELECT COUNT(1) FROM Categoria";
            await using var commandTotal = new SqlCommand(sqlTotal, connection);
            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            const string sqlPaginado = @"
                WITH Dados AS
                (
                    SELECT
                        codigo AS CodCategoria,
                        nome AS NomeCategoria,
                        ROW_NUMBER() OVER (ORDER BY nome) AS RowNum
                    FROM Categoria
                )
                SELECT CodCategoria, NomeCategoria
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum";

            await using var commandPaginado = new SqlCommand(sqlPaginado, connection);
            commandPaginado.Parameters.AddWithValue("@RowStart", rowStart);
            commandPaginado.Parameters.AddWithValue("@RowEnd", rowEnd);
            await using var readerPaginado = await commandPaginado.ExecuteReaderAsync();

            while (await readerPaginado.ReadAsync())
            {
                categorias.Add(new Categoria
                {
                    CodCategoria = Convert.ToInt32(readerPaginado.GetValue(0)),
                    NomeCategoria = readerPaginado.IsDBNull(1) ? string.Empty : readerPaginado.GetString(1)
                });
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<Categoria>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = categorias
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar categorias no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar categorias: {ex.Message}");
        }
    }
}

using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/fornecedor")]
public class FornecedorController : ControllerBase
{
    private readonly ConnectionStringProvider _connectionStringProvider;

    public FornecedorController(ConnectionStringProvider connectionStringProvider)
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

            var fornecedores = new List<Fornecedor>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            if (!paginacao.HasPagination)
            {
                const string sqlSemPaginacao = @"
                    SELECT cod_for, nome_for
                    FROM Fornecedor
                    ORDER BY nome_for";

                await using var commandSemPaginacao = new SqlCommand(sqlSemPaginacao, connection);
                await using var readerSemPaginacao = await commandSemPaginacao.ExecuteReaderAsync();

                while (await readerSemPaginacao.ReadAsync())
                {
                    fornecedores.Add(new Fornecedor
                    {
                        CodFor = Convert.ToInt32(readerSemPaginacao.GetValue(0)),
                        NomeFor = readerSemPaginacao.IsDBNull(1) ? string.Empty : readerSemPaginacao.GetString(1)
                    });
                }

                return Ok(fornecedores);
            }

            if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
            {
                return BadRequest(error);
            }

            const string sqlTotal = "SELECT COUNT(1) FROM Fornecedor";
            await using var commandTotal = new SqlCommand(sqlTotal, connection);
            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            const string sqlPaginado = @"
                WITH Dados AS
                (
                    SELECT
                        cod_for,
                        nome_for,
                        ROW_NUMBER() OVER (ORDER BY nome_for) AS RowNum
                    FROM Fornecedor
                )
                SELECT cod_for, nome_for
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum";

            await using var commandPaginado = new SqlCommand(sqlPaginado, connection);
            commandPaginado.Parameters.AddWithValue("@RowStart", rowStart);
            commandPaginado.Parameters.AddWithValue("@RowEnd", rowEnd);
            await using var readerPaginado = await commandPaginado.ExecuteReaderAsync();

            while (await readerPaginado.ReadAsync())
            {
                fornecedores.Add(new Fornecedor
                {
                    CodFor = Convert.ToInt32(readerPaginado.GetValue(0)),
                    NomeFor = readerPaginado.IsDBNull(1) ? string.Empty : readerPaginado.GetString(1)
                });
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<Fornecedor>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = fornecedores
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar fornecedores no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar fornecedores: {ex.Message}");
        }
    }
}

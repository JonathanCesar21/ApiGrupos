using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/cor")]
public class CorController : ControllerBase
{
    private readonly ConnectionStringProvider _connectionStringProvider;

    public CorController(ConnectionStringProvider connectionStringProvider)
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

            var cores = new List<Cor>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            if (!paginacao.HasPagination)
            {
                const string sqlSemPaginacao = @"
                    SELECT codigo, descricao
                    FROM Cor
                    ORDER BY descricao";

                await using var commandSemPaginacao = new SqlCommand(sqlSemPaginacao, connection);
                await using var readerSemPaginacao = await commandSemPaginacao.ExecuteReaderAsync();

                while (await readerSemPaginacao.ReadAsync())
                {
                    cores.Add(new Cor
                    {
                        CodCor = Convert.ToInt32(readerSemPaginacao.GetValue(0)),
                        NomeCor = readerSemPaginacao.IsDBNull(1) ? string.Empty : readerSemPaginacao.GetString(1)
                    });
                }

                return Ok(cores);
            }

            if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
            {
                return BadRequest(error);
            }

            const string sqlTotal = "SELECT COUNT(1) FROM Cor";
            await using var commandTotal = new SqlCommand(sqlTotal, connection);
            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            const string sqlPaginado = @"
                WITH Dados AS
                (
                    SELECT
                        codigo,
                        descricao,
                        ROW_NUMBER() OVER (ORDER BY descricao) AS RowNum
                    FROM Cor
                )
                SELECT codigo, descricao
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum";

            await using var commandPaginado = new SqlCommand(sqlPaginado, connection);
            commandPaginado.Parameters.AddWithValue("@RowStart", rowStart);
            commandPaginado.Parameters.AddWithValue("@RowEnd", rowEnd);
            await using var readerPaginado = await commandPaginado.ExecuteReaderAsync();

            while (await readerPaginado.ReadAsync())
            {
                cores.Add(new Cor
                {
                    CodCor = Convert.ToInt32(readerPaginado.GetValue(0)),
                    NomeCor = readerPaginado.IsDBNull(1) ? string.Empty : readerPaginado.GetString(1)
                });
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<Cor>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = cores
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar cores no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar cores: {ex.Message}");
        }
    }
}

using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubgruposController : ControllerBase
{
    private readonly ConnectionStringProvider _connectionStringProvider;

    public SubgruposController(ConnectionStringProvider connectionStringProvider)
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

            var subgrupos = new List<Subgrupo>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            if (!paginacao.HasPagination)
            {
                const string sqlSemPaginacao = @"
                    SELECT codsubgrupo, nomesubgrupo, CodGrupo
                    FROM Subgrupos
                    ORDER BY nomesubgrupo";

                await using var commandSemPaginacao = new SqlCommand(sqlSemPaginacao, connection);
                await using var readerSemPaginacao = await commandSemPaginacao.ExecuteReaderAsync();

                while (await readerSemPaginacao.ReadAsync())
                {
                    subgrupos.Add(new Subgrupo
                    {
                        Id = Convert.ToInt32(readerSemPaginacao.GetValue(0)),
                        Nome = readerSemPaginacao.IsDBNull(1) ? string.Empty : readerSemPaginacao.GetString(1),
                        GrupoCodigo = readerSemPaginacao.IsDBNull(2) ? null : Convert.ToInt32(readerSemPaginacao.GetValue(2))
                    });
                }

                return Ok(subgrupos);
            }

            if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
            {
                return BadRequest(error);
            }

            const string sqlTotal = "SELECT COUNT(1) FROM Subgrupos";
            await using var commandTotal = new SqlCommand(sqlTotal, connection);
            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            const string sqlPaginado = @"
                WITH Dados AS
                (
                    SELECT
                        codsubgrupo,
                        nomesubgrupo,
                        CodGrupo,
                        ROW_NUMBER() OVER (ORDER BY nomesubgrupo) AS RowNum
                    FROM Subgrupos
                )
                SELECT codsubgrupo, nomesubgrupo, CodGrupo
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum";

            await using var commandPaginado = new SqlCommand(sqlPaginado, connection);
            commandPaginado.Parameters.AddWithValue("@RowStart", rowStart);
            commandPaginado.Parameters.AddWithValue("@RowEnd", rowEnd);
            await using var readerPaginado = await commandPaginado.ExecuteReaderAsync();

            while (await readerPaginado.ReadAsync())
            {
                subgrupos.Add(new Subgrupo
                {
                    Id = Convert.ToInt32(readerPaginado.GetValue(0)),
                    Nome = readerPaginado.IsDBNull(1) ? string.Empty : readerPaginado.GetString(1),
                    GrupoCodigo = readerPaginado.IsDBNull(2) ? null : Convert.ToInt32(readerPaginado.GetValue(2))
                });
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<Subgrupo>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = subgrupos
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar subgrupos no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar subgrupos: {ex.Message}");
        }
    }

    [HttpGet("por-grupo/{grupoCodigo:int}")]
    public async Task<ActionResult> GetPorGrupo(int grupoCodigo, [FromQuery] PaginacaoQuery paginacao)
    {
        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var subgrupos = new List<Subgrupo>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            if (!paginacao.HasPagination)
            {
                const string sqlSemPaginacao = @"
                    SELECT codsubgrupo, nomesubgrupo, CodGrupo
                    FROM Subgrupos
                    WHERE CodGrupo = @GrupoCodigo
                    ORDER BY nomesubgrupo";

                await using var commandSemPaginacao = new SqlCommand(sqlSemPaginacao, connection);
                commandSemPaginacao.Parameters.AddWithValue("@GrupoCodigo", grupoCodigo);
                await using var readerSemPaginacao = await commandSemPaginacao.ExecuteReaderAsync();

                while (await readerSemPaginacao.ReadAsync())
                {
                    subgrupos.Add(new Subgrupo
                    {
                        Id = Convert.ToInt32(readerSemPaginacao.GetValue(0)),
                        Nome = readerSemPaginacao.IsDBNull(1) ? string.Empty : readerSemPaginacao.GetString(1),
                        GrupoCodigo = readerSemPaginacao.IsDBNull(2) ? null : Convert.ToInt32(readerSemPaginacao.GetValue(2))
                    });
                }

                return Ok(subgrupos);
            }

            if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
            {
                return BadRequest(error);
            }

            const string sqlTotal = @"
                SELECT COUNT(1)
                FROM Subgrupos
                WHERE CodGrupo = @GrupoCodigo";

            await using var commandTotal = new SqlCommand(sqlTotal, connection);
            commandTotal.Parameters.AddWithValue("@GrupoCodigo", grupoCodigo);
            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            const string sqlPaginado = @"
                WITH Dados AS
                (
                    SELECT
                        codsubgrupo,
                        nomesubgrupo,
                        CodGrupo,
                        ROW_NUMBER() OVER (ORDER BY nomesubgrupo) AS RowNum
                    FROM Subgrupos
                    WHERE CodGrupo = @GrupoCodigo
                )
                SELECT codsubgrupo, nomesubgrupo, CodGrupo
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum";

            await using var commandPaginado = new SqlCommand(sqlPaginado, connection);
            commandPaginado.Parameters.AddWithValue("@GrupoCodigo", grupoCodigo);
            commandPaginado.Parameters.AddWithValue("@RowStart", rowStart);
            commandPaginado.Parameters.AddWithValue("@RowEnd", rowEnd);

            await using var readerPaginado = await commandPaginado.ExecuteReaderAsync();

            while (await readerPaginado.ReadAsync())
            {
                subgrupos.Add(new Subgrupo
                {
                    Id = Convert.ToInt32(readerPaginado.GetValue(0)),
                    Nome = readerPaginado.IsDBNull(1) ? string.Empty : readerPaginado.GetString(1),
                    GrupoCodigo = readerPaginado.IsDBNull(2) ? null : Convert.ToInt32(readerPaginado.GetValue(2))
                });
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<Subgrupo>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = subgrupos
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar subgrupos por grupo no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar subgrupos por grupo: {ex.Message}");
        }
    }
}

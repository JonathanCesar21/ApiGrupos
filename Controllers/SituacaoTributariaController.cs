using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/situacao-tributaria")]
public class SituacaoTributariaController : ControllerBase
{
    private readonly ConnectionStringProvider _connectionStringProvider;

    public SituacaoTributariaController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get([FromQuery] PaginacaoQuery paginacao)
    {
        return await GetSituacoesAsync(
            paginacao,
            "SELECT Codigo, Descricao FROM SituacaoTributaria",
            "SELECT COUNT(1) FROM SituacaoTributaria",
            "Codigo, Descricao AS DescricaoFormatada",
            "Descricao",
            "situacoes tributarias",
            reader => new SituacaoTributaria
            {
                CodSituacaoTributaria = Convert.ToInt32(reader.GetValue(0)),
                Descricao = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
    }

    [HttpGet("rpa")]
    public async Task<ActionResult> GetRpa([FromQuery] PaginacaoQuery paginacao)
    {
        return await GetSituacoesAsync(
            paginacao,
            "SELECT Codigo, CAST(Codigo AS VARCHAR(20)) + ' - ' + Descricao FROM SituacaoTributaria",
            "SELECT COUNT(1) FROM SituacaoTributaria",
            "Codigo, CAST(Codigo AS VARCHAR(20)) + ' - ' + Descricao AS DescricaoFormatada",
            "Descricao",
            "situacoes tributarias RPA",
            reader => new SituacaoTributariaRpa
            {
                CodSituacaoTributariaRpa = Convert.ToInt32(reader.GetValue(0)),
                NomeSituacaoTributariaRpa = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            });
    }

    [HttpGet("simples")]
    public async Task<ActionResult> GetSimples([FromQuery] PaginacaoQuery paginacao)
    {
        return await GetSituacoesAsync(
            paginacao,
            "SELECT Codigo, CAST(Codigo AS VARCHAR(20)) + ' - ' + Descricao FROM SituacaoTributaria WHERE SN = '1'",
            "SELECT COUNT(1) FROM SituacaoTributaria WHERE SN = '1'",
            "Codigo, CAST(Codigo AS VARCHAR(20)) + ' - ' + Descricao AS DescricaoFormatada",
            "Descricao",
            "situacoes tributarias do simples",
            reader => new SituacaoTributariaSimples
            {
                CodSituacaoTributariaSimples = Convert.ToInt32(reader.GetValue(0)),
                NomeSituacaoTributariaSimples = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
            },
            "WHERE SN = '1'");
    }

    private async Task<ActionResult> GetSituacoesAsync<T>(
        PaginacaoQuery paginacao,
        string sqlSemPaginacao,
        string sqlTotal,
        string selectPaginado,
        string orderBy,
        string descricaoRecurso,
        Func<SqlDataReader, T> map,
        string whereClause = "")
    {
        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var itens = new List<T>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            if (!paginacao.HasPagination)
            {
                await using var commandSemPaginacao = new SqlCommand($@"
                    {sqlSemPaginacao}
                    ORDER BY {orderBy}", connection);
                await using var readerSemPaginacao = await commandSemPaginacao.ExecuteReaderAsync();

                while (await readerSemPaginacao.ReadAsync())
                {
                    itens.Add(map(readerSemPaginacao));
                }

                return Ok(itens);
            }

            if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
            {
                return BadRequest(error);
            }

            await using var commandTotal = new SqlCommand(sqlTotal, connection);
            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            var sqlPaginado = $@"
                WITH Dados AS
                (
                    SELECT
                        {selectPaginado},
                        ROW_NUMBER() OVER (ORDER BY {orderBy}) AS RowNum
                    FROM SituacaoTributaria
                    {whereClause}
                )
                SELECT Codigo, DescricaoFormatada
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum";

            await using var commandPaginado = new SqlCommand(sqlPaginado, connection);
            commandPaginado.Parameters.AddWithValue("@RowStart", rowStart);
            commandPaginado.Parameters.AddWithValue("@RowEnd", rowEnd);
            await using var readerPaginado = await commandPaginado.ExecuteReaderAsync();

            while (await readerPaginado.ReadAsync())
            {
                itens.Add(map(readerPaginado));
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<T>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = itens
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar {descricaoRecurso} no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar {descricaoRecurso}: {ex.Message}");
        }
    }
}

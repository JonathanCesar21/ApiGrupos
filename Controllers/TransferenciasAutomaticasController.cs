using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/transferencias-automaticas")]
public class TransferenciasAutomaticasController : ControllerBase
{
    private readonly ConnectionStringProvider _connectionStringProvider;

    public TransferenciasAutomaticasController(ConnectionStringProvider connectionStringProvider)
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

            var transferencias = new List<TransferenciaAutomatica>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            if (!paginacao.HasPagination)
            {
                const string sqlSemPaginacao = @"
                    SELECT
                        t.referencia,
                        p.secao AS CodSecao,
                        s.Descricao AS DescSecao,
                        t.codcor,
                        c.Descricao AS DescCor,
                        t.CodNumero,
                        n.Descricao AS DescNumero,
                        t.Quant,
                        t.NotaFiscal,
                        t.Destino AS Loja
                    FROM Transferencia t
                    INNER JOIN Produtos p
                        ON p.referencia = t.referencia
                    LEFT JOIN Secao s
                        ON s.Codigo = p.secao
                    LEFT JOIN Cor c
                        ON c.Codigo = t.codcor
                    LEFT JOIN Numeracao n
                        ON n.Codigo = t.CodNumero
                    WHERE
                        t.Automatica = '1'
                        AND t.statusimpressao = '1'
                        AND t.DataAuto IS NULL
                    ORDER BY t.referencia, t.codcor, t.CodNumero";

                await using var commandSemPaginacao = new SqlCommand(sqlSemPaginacao, connection);
                await using var readerSemPaginacao = await commandSemPaginacao.ExecuteReaderAsync();

                while (await readerSemPaginacao.ReadAsync())
                {
                    transferencias.Add(new TransferenciaAutomatica
                    {
                        Referencia = readerSemPaginacao.IsDBNull(0) ? string.Empty : readerSemPaginacao.GetString(0),
                        CodSecao = readerSemPaginacao.IsDBNull(1) ? null : Convert.ToInt32(readerSemPaginacao.GetValue(1)),
                        DescSecao = readerSemPaginacao.IsDBNull(2) ? string.Empty : readerSemPaginacao.GetString(2),
                        CodCor = readerSemPaginacao.IsDBNull(3) ? null : Convert.ToInt32(readerSemPaginacao.GetValue(3)),
                        DescCor = readerSemPaginacao.IsDBNull(4) ? string.Empty : readerSemPaginacao.GetString(4),
                        CodNumero = readerSemPaginacao.IsDBNull(5) ? null : Convert.ToInt32(readerSemPaginacao.GetValue(5)),
                        DescNumero = readerSemPaginacao.IsDBNull(6) ? string.Empty : readerSemPaginacao.GetString(6),
                        Quant = readerSemPaginacao.IsDBNull(7) ? 0 : Convert.ToInt32(readerSemPaginacao.GetValue(7)),
                        NotaFiscal = readerSemPaginacao.IsDBNull(8) ? string.Empty : readerSemPaginacao.GetValue(8).ToString() ?? string.Empty,
                        Loja = readerSemPaginacao.IsDBNull(9) ? string.Empty : readerSemPaginacao.GetValue(9).ToString() ?? string.Empty
                    });
                }

                return Ok(transferencias);
            }

            if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
            {
                return BadRequest(error);
            }

            const string sqlTotal = @"
                SELECT COUNT(1)
                FROM Transferencia t
                INNER JOIN Produtos p
                    ON p.referencia = t.referencia
                LEFT JOIN Secao s
                    ON s.Codigo = p.secao
                LEFT JOIN Cor c
                    ON c.Codigo = t.codcor
                LEFT JOIN Numeracao n
                    ON n.Codigo = t.CodNumero
                WHERE
                    t.Automatica = '1'
                    AND t.statusimpressao = '1'
                    AND t.DataAuto IS NULL";

            await using var commandTotal = new SqlCommand(sqlTotal, connection);
            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync());

            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            const string sqlPaginado = @"
                WITH Dados AS
                (
                    SELECT
                        t.referencia,
                        p.secao AS CodSecao,
                        s.Descricao AS DescSecao,
                        t.codcor,
                        c.Descricao AS DescCor,
                        t.CodNumero,
                        n.Descricao AS DescNumero,
                        t.Quant,
                        t.NotaFiscal,
                        t.Destino AS Loja,
                        ROW_NUMBER() OVER (ORDER BY t.referencia, t.codcor, t.CodNumero) AS RowNum
                    FROM Transferencia t
                    INNER JOIN Produtos p
                        ON p.referencia = t.referencia
                    LEFT JOIN Secao s
                        ON s.Codigo = p.secao
                    LEFT JOIN Cor c
                        ON c.Codigo = t.codcor
                    LEFT JOIN Numeracao n
                        ON n.Codigo = t.CodNumero
                    WHERE
                        t.Automatica = '1'
                        AND t.statusimpressao = '1'
                        AND t.DataAuto IS NULL
                )
                SELECT referencia, CodSecao, DescSecao, codcor, DescCor, CodNumero, DescNumero, Quant, NotaFiscal, Loja
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum";

            await using var commandPaginado = new SqlCommand(sqlPaginado, connection);
            commandPaginado.Parameters.AddWithValue("@RowStart", rowStart);
            commandPaginado.Parameters.AddWithValue("@RowEnd", rowEnd);
            await using var readerPaginado = await commandPaginado.ExecuteReaderAsync();

            while (await readerPaginado.ReadAsync())
            {
                transferencias.Add(new TransferenciaAutomatica
                {
                    Referencia = readerPaginado.IsDBNull(0) ? string.Empty : readerPaginado.GetString(0),
                    CodSecao = readerPaginado.IsDBNull(1) ? null : Convert.ToInt32(readerPaginado.GetValue(1)),
                    DescSecao = readerPaginado.IsDBNull(2) ? string.Empty : readerPaginado.GetString(2),
                    CodCor = readerPaginado.IsDBNull(3) ? null : Convert.ToInt32(readerPaginado.GetValue(3)),
                    DescCor = readerPaginado.IsDBNull(4) ? string.Empty : readerPaginado.GetString(4),
                    CodNumero = readerPaginado.IsDBNull(5) ? null : Convert.ToInt32(readerPaginado.GetValue(5)),
                    DescNumero = readerPaginado.IsDBNull(6) ? string.Empty : readerPaginado.GetString(6),
                    Quant = readerPaginado.IsDBNull(7) ? 0 : Convert.ToInt32(readerPaginado.GetValue(7)),
                    NotaFiscal = readerPaginado.IsDBNull(8) ? string.Empty : readerPaginado.GetValue(8).ToString() ?? string.Empty,
                    Loja = readerPaginado.IsDBNull(9) ? string.Empty : readerPaginado.GetValue(9).ToString() ?? string.Empty
                });
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<TransferenciaAutomatica>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = transferencias
            });
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar transferencias automaticas no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar transferencias automaticas: {ex.Message}");
        }
    }
}

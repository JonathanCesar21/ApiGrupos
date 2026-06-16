using System.Data;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/consulta-entradas")]
public class ConsultaEntradasController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ConsultaEntradasController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get(
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        CancellationToken cancellationToken)
    {
        if (!dataInicio.HasValue || !dataFim.HasValue)
        {
            return BadRequest("Os parametros 'dataInicio' e 'dataFim' sao obrigatorios.");
        }

        var inicio = dataInicio.Value.Date;
        var fim = dataFim.Value.Date;

        if (fim < inicio)
        {
            return BadRequest("O parametro 'dataFim' deve ser maior ou igual a 'dataInicio'.");
        }

        if (fim == DateTime.MaxValue.Date)
        {
            return BadRequest("O parametro 'dataFim' excede o limite suportado.");
        }

        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var fimExclusivo = fim.AddDays(1);
            var entradas = new List<Dictionary<string, object?>>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                WITH CTE AS
                (
                    SELECT
                        c.*,
                        pb.cor AS Cor,
                        f.nome_for AS Fornecedor,
                        p.subgrupo AS codSubGrupo,
                        p.Venda,
                        sg.nomesubgrupo AS SubGrupo,
                        g.nomeGrupo AS Grupo,
                        n.Descricao AS numero,
                        p.Colecao,
                        p.Categoria,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY
                                c.NotaFiscal,
                                c.Referencia,
                                c.Loja,
                                n.Descricao,
                                f.nome_for,
                                p.subgrupo,
                                p.Venda,
                                sg.nomesubgrupo,
                                g.nomeGrupo,
                                p.Colecao,
                                p.Categoria,
                                c.barras
                            ORDER BY c.Data DESC
                        ) AS rn
                    FROM centrada AS c
                    INNER JOIN Numeracao n
                        ON c.CodNumero = n.codigo
                    INNER JOIN fornecedor AS f
                        ON c.codfor = f.cod_for
                    INNER JOIN produtos AS p
                        ON c.CodProd = p.codigo
                    INNER JOIN SubGrupos AS sg
                        ON p.subgrupo = sg.codsubgrupo
                    INNER JOIN grupos AS g
                        ON p.Grupo = g.codGrupo
                    LEFT JOIN ProdutoBarras pb
                        ON pb.barras = c.barras
                       AND pb.referencia = c.Referencia
                    WHERE c.Data >= @DataInicio
                      AND c.Data < @DataFimExclusivo
                )
                SELECT *
                FROM CTE
                WHERE rn = 1
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            command.Parameters.Add("@DataInicio", SqlDbType.DateTime).Value = inicio;
            command.Parameters.Add("@DataFimExclusivo", SqlDbType.DateTime).Value = fimExclusivo;

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var entrada = new Dictionary<string, object?>(reader.FieldCount);

                for (var columnIndex = 0; columnIndex < reader.FieldCount; columnIndex++)
                {
                    entrada[reader.GetName(columnIndex)] = await reader.IsDBNullAsync(columnIndex, cancellationToken)
                        ? null
                        : reader.GetValue(columnIndex);
                }

                entradas.Add(entrada);
            }

            return Ok(entradas);
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
                $"Erro ao consultar entradas no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar entradas: {ex.Message}");
        }
    }
}

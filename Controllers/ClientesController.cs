using System.Data;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ClientesController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet]
    public async Task<ActionResult> Get(
        [FromQuery] PaginacaoQuery paginacao,
        [FromQuery] string? busca = null,
        [FromQuery] int? loja = null,
        CancellationToken cancellationToken = default)
    {
        if (!paginacao.TryResolve(out var page, out var pageSize, out var error))
        {
            return BadRequest(error);
        }

        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            var clientes = new List<Cliente>();
            var buscaNormalizada = string.IsNullOrWhiteSpace(busca) ? null : busca.Trim();
            var hasCodigoBusca = int.TryParse(buscaNormalizada, out var codigoBusca);

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var where = BuildWhere(buscaNormalizada, loja, hasCodigoBusca);

            var sqlTotal = $"""
                SELECT COUNT(1)
                FROM Clientes c
                INNER JOIN Cidades cid
                    ON c.Cidade = cid.Seq
                {where}
                """;

            await using var commandTotal = new SqlCommand(sqlTotal, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };
            AddCommonParameters(commandTotal, buscaNormalizada, loja, hasCodigoBusca, codigoBusca);
            var total = Convert.ToInt32(await commandTotal.ExecuteScalarAsync(cancellationToken));

            var rowStart = ((page - 1) * pageSize) + 1;
            var rowEnd = rowStart + pageSize - 1;

            var sql = $"""
                WITH Dados AS
                (
                    SELECT
                        c.Codigo,
                        c.nome,
                        c.Bairro,
                        cid.Cidade AS NomeCidade,
                        c.dtNascimento,
                        CASE c.Sexo
                            WHEN 0 THEN 'Homem'
                            WHEN 1 THEN 'Mulher'
                            ELSE 'Nao informado'
                        END AS Sexo,
                        c.CodGrupo,
                        c.Limite,
                        c.Renda,
                        c.Idade,
                        c.Loja,
                        c.Fone,
                        c.FoneRef1 AS FoneReferencia1,
                        c.FoneRef2 AS FoneReferencia2,
                        ROW_NUMBER() OVER (ORDER BY c.nome, c.Codigo) AS RowNum
                    FROM Clientes c
                    INNER JOIN Cidades cid
                        ON c.Cidade = cid.Seq
                    {where}
                )
                SELECT
                    Codigo,
                    nome,
                    Bairro,
                    NomeCidade,
                    dtNascimento,
                    Sexo,
                    CodGrupo,
                    Limite,
                    Renda,
                    Idade,
                    Loja,
                    Fone,
                    FoneReferencia1,
                    FoneReferencia2
                FROM Dados
                WHERE RowNum BETWEEN @RowStart AND @RowEnd
                ORDER BY RowNum
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };
            AddCommonParameters(command, buscaNormalizada, loja, hasCodigoBusca, codigoBusca);
            command.Parameters.Add("@RowStart", SqlDbType.Int).Value = rowStart;
            command.Parameters.Add("@RowEnd", SqlDbType.Int).Value = rowEnd;

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                clientes.Add(ReadCliente(reader));
            }

            var totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize);

            return Ok(new PaginacaoResposta<Cliente>
            {
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = totalPages,
                Items = clientes
            });
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
                $"Erro ao consultar clientes no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar clientes: {ex.Message}");
        }
    }

    [HttpGet("{codigo:int}")]
    public async Task<ActionResult> GetByCodigo(int codigo, CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Credenciais do banco nao configuradas. Acesse /configuracao para informar usuario e senha.");
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT
                    c.Codigo,
                    c.nome,
                    c.Bairro,
                    cid.Cidade AS NomeCidade,
                    c.dtNascimento,
                    CASE c.Sexo
                        WHEN 0 THEN 'Homem'
                        WHEN 1 THEN 'Mulher'
                        ELSE 'Nao informado'
                    END AS Sexo,
                    c.CodGrupo,
                    c.Limite,
                    c.Renda,
                    c.Idade,
                    c.Loja,
                    c.Fone,
                    c.FoneRef1 AS FoneReferencia1,
                    c.FoneRef2 AS FoneReferencia2
                FROM Clientes c
                INNER JOIN Cidades cid
                    ON c.Cidade = cid.Seq
                WHERE c.Codigo = @Codigo
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };
            command.Parameters.Add("@Codigo", SqlDbType.Int).Value = codigo;

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
            {
                return NotFound();
            }

            return Ok(ReadCliente(reader));
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
                $"Erro ao consultar cliente no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar cliente: {ex.Message}");
        }
    }

    private static string BuildWhere(string? busca, int? loja, bool hasCodigoBusca)
    {
        var filters = new List<string>();

        if (!string.IsNullOrWhiteSpace(busca))
        {
            var buscaFilter = """
                (
                    c.nome LIKE @Busca
                    OR c.Bairro LIKE @Busca
                    OR cid.Cidade LIKE @Busca
                    OR c.Fone LIKE @Busca
                    OR c.FoneRef1 LIKE @Busca
                    OR c.FoneRef2 LIKE @Busca
                """;

            if (hasCodigoBusca)
            {
                buscaFilter += " OR c.Codigo = @CodigoBusca";
            }

            buscaFilter += ")";
            filters.Add(buscaFilter);
        }

        if (loja.HasValue)
        {
            filters.Add("c.Loja = @Loja");
        }

        return filters.Count == 0
            ? string.Empty
            : "WHERE " + string.Join(" AND ", filters);
    }

    private static void AddCommonParameters(
        SqlCommand command,
        string? busca,
        int? loja,
        bool hasCodigoBusca,
        int codigoBusca)
    {
        if (!string.IsNullOrWhiteSpace(busca))
        {
            command.Parameters.Add("@Busca", SqlDbType.NVarChar, 200).Value = $"%{busca}%";

            if (hasCodigoBusca)
            {
                command.Parameters.Add("@CodigoBusca", SqlDbType.Int).Value = codigoBusca;
            }
        }

        if (loja.HasValue)
        {
            command.Parameters.Add("@Loja", SqlDbType.Int).Value = loja.Value;
        }
    }

    private static Cliente ReadCliente(SqlDataReader reader)
    {
        return new Cliente
        {
            Codigo = Convert.ToInt32(reader.GetValue(0)),
            Nome = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Bairro = ReadString(reader, 2),
            NomeCidade = ReadString(reader, 3),
            DtNascimento = ReadDateTime(reader, 4),
            Sexo = reader.IsDBNull(5) ? "Nao informado" : reader.GetString(5),
            CodGrupo = ReadInt32(reader, 6),
            Limite = ReadDecimal(reader, 7),
            Renda = ReadDecimal(reader, 8),
            Idade = ReadInt32(reader, 9),
            Loja = ReadInt32(reader, 10),
            Fone = ReadString(reader, 11),
            FoneReferencia1 = ReadString(reader, 12),
            FoneReferencia2 = ReadString(reader, 13)
        };
    }

    private static string? ReadString(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal).ToString();
    }

    private static int? ReadInt32(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static decimal? ReadDecimal(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static DateTime? ReadDateTime(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));
    }
}

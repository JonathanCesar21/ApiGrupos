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
    public async Task<ActionResult<IEnumerable<Subgrupo>>> Get()
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

            const string sql = @"
                SELECT codsubgrupo, nomesubgrupo, CodGrupo
                FROM Subgrupos
                ORDER BY nomesubgrupo";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                subgrupos.Add(new Subgrupo
                {
                    Id = Convert.ToInt32(reader.GetValue(0)),
                    Nome = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    GrupoCodigo = reader.IsDBNull(2) ? null : Convert.ToInt32(reader.GetValue(2))
                });
            }

            return Ok(subgrupos);
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
    public async Task<ActionResult<IEnumerable<Subgrupo>>> GetPorGrupo(int grupoCodigo)
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

            const string sql = @"
                SELECT codsubgrupo, nomesubgrupo, CodGrupo
                FROM Subgrupos
                WHERE CodGrupo = @GrupoCodigo
                ORDER BY nomesubgrupo";

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@GrupoCodigo", grupoCodigo);

            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                subgrupos.Add(new Subgrupo
                {
                    Id = Convert.ToInt32(reader.GetValue(0)),
                    Nome = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    GrupoCodigo = reader.IsDBNull(2) ? null : Convert.ToInt32(reader.GetValue(2))
                });
            }

            return Ok(subgrupos);
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

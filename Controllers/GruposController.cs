using ApiGrupos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GruposController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public GruposController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Grupo>>> Get()
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "Connection string 'DefaultConnection' não foi configurada.");
            }

            var grupos = new List<Grupo>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Nomes reais aplicados:
            // Tabela grupos: Grupos
            // Código do grupo: codgrupo
            // Nome do grupo: nomegrupo
            const string sql = @"
                SELECT codgrupo, nomegrupo
                FROM Grupos
                ORDER BY nomegrupo";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                grupos.Add(new Grupo
                {
                    Id = Convert.ToInt32(reader.GetValue(0)),
                    Nome = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                });
            }

            return Ok(grupos);
        }
        catch (SqlException ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro ao consultar grupos no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar grupos: {ex.Message}");
        }
    }
}


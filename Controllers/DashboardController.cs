using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public class DashboardController(ConnectionStringProvider connectionStringProvider, ILogger<DashboardController> logger) : ControllerBase
{
    [HttpGet("recebimentos")]
    [ProducesResponseType(typeof(List<DashboardRecebimento>), StatusCodes.Status200OK)]
    public Task<ActionResult> Recebimentos([FromQuery] DashboardRecebimentosQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardRecebimento>(() => DashboardSql.Recebimentos(query), cancellationToken);

    [HttpGet("titulos")]
    [ProducesResponseType(typeof(List<DashboardTitulo>), StatusCodes.Status200OK)]
    public Task<ActionResult> Titulos([FromQuery] DashboardTitulosQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardTitulo>(() => DashboardSql.Titulos(query), cancellationToken);

    [HttpGet("metas-lojas")]
    [ProducesResponseType(typeof(List<DashboardMetaLoja>), StatusCodes.Status200OK)]
    public Task<ActionResult> MetasLojas([FromQuery] DashboardMetasQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardMetaLoja>(() => DashboardSql.MetasLojas(query), cancellationToken);

    [HttpGet("metas-vendedores")]
    [ProducesResponseType(typeof(List<DashboardMetaVendedor>), StatusCodes.Status200OK)]
    public Task<ActionResult> MetasVendedores([FromQuery] DashboardMetasQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardMetaVendedor>(() => DashboardSql.MetasVendedores(query), cancellationToken);

    [HttpGet("vendedores")]
    [ProducesResponseType(typeof(List<DashboardVendedor>), StatusCodes.Status200OK)]
    public Task<ActionResult> Vendedores([FromQuery] DashboardVendedoresQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardVendedor>(() => DashboardSql.Vendedores(query), cancellationToken);

    [HttpGet("entradas-a-confirmar")]
    [ProducesResponseType(typeof(List<DashboardEntradaConfirmar>), StatusCodes.Status200OK)]
    public Task<ActionResult> EntradasConfirmar([FromQuery] DashboardPeriodoQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardEntradaConfirmar>(() => DashboardSql.EntradasConfirmar(query), cancellationToken);

    [HttpGet("transferencias")]
    [ProducesResponseType(typeof(List<DashboardTransferencia>), StatusCodes.Status200OK)]
    public Task<ActionResult> Transferencias([FromQuery] DashboardTransferenciasQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardTransferencia>(() => DashboardSql.Transferencias(query), cancellationToken);

    [HttpGet("ativacoes")]
    [ProducesResponseType(typeof(List<DashboardAtivacao>), StatusCodes.Status200OK)]
    public Task<ActionResult> Ativacoes([FromQuery] DashboardPeriodoQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardAtivacao>(() => DashboardSql.Ativacoes(query), cancellationToken);

    [HttpGet("referencias")]
    [ProducesResponseType(typeof(List<DashboardReferencia>), StatusCodes.Status200OK)]
    public Task<ActionResult> Referencias([FromQuery] DashboardReferenciasQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardReferencia>(() => DashboardSql.Referencias(query), cancellationToken);

    [HttpGet("terceiros")]
    [ProducesResponseType(typeof(List<DashboardTerceiro>), StatusCodes.Status200OK)]
    public Task<ActionResult> Terceiros([FromQuery] DashboardTerceirosQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardTerceiro>(() => DashboardSql.Terceiros(query), cancellationToken);

    [HttpGet("vendas-agregado")]
    [ProducesResponseType(typeof(List<DashboardVendaAgregada>), StatusCodes.Status200OK)]
    public Task<ActionResult> VendasAgregado([FromQuery] DashboardPeriodoQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardVendaAgregada>(() => DashboardSql.VendasAgregado(query), cancellationToken);

    [HttpGet("entradas-agregado")]
    [ProducesResponseType(typeof(List<DashboardEntradaAgregada>), StatusCodes.Status200OK)]
    public Task<ActionResult> EntradasAgregado([FromQuery] DashboardPeriodoQuery query, CancellationToken cancellationToken = default) =>
        Execute<DashboardEntradaAgregada>(() => DashboardSql.EntradasAgregado(query), cancellationToken);

    private async Task<ActionResult> Execute<T>(Func<DashboardCommand> build, CancellationToken cancellationToken) where T : new()
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Sem chave unica confirmada nao oferecemos paginacao de fatos. Nunca
            // ignorar page/pageSize silenciosamente, pois isso repetiria a fotografia.
            if (ControllerContext.HttpContext?.Request.Query.Keys.Any(k =>
                k.Equals("page", StringComparison.OrdinalIgnoreCase) || k.Equals("pageSize", StringComparison.OrdinalIgnoreCase)) == true)
                return BadRequest("As rotas dashboard retornam arrays completos, sem paginacao. Use os filtros documentados.");
            var plan = build();
            var connectionString = connectionStringProvider.GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
                return StatusCode(StatusCodes.Status503ServiceUnavailable, "Credenciais do banco nao configuradas. Acesse /configuracao.");

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(plan.Sql, connection) { CommandTimeout = DashboardSql.TimeoutSeconds };
            command.Parameters.AddRange(plan.Parameters.ToArray());
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var items = new List<T>();
            while (await reader.ReadAsync(cancellationToken)) items.Add(DashboardReader.Read<T>(reader));
            return Ok(items);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return new EmptyResult(); }
        catch (SqlException) when (cancellationToken.IsCancellationRequested) { return new EmptyResult(); }
        catch (SqlException ex) when (ex.Number == -2)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, $"A consulta excedeu o timeout de {DashboardSql.TimeoutSeconds} segundos.");
        }
        catch (Exception ex)
        {
            // Nao devolve SQL, parametros ou dados de clientes na resposta/log.
            logger.LogError("Falha na consulta dashboard {Dto}: {ErrorType}; SQL numero {SqlNumber}.",
                typeof(T).Name, ex.GetType().Name, (ex as SqlException)?.Number);
            return StatusCode(StatusCodes.Status500InternalServerError, "Erro ao consultar dados do dashboard.");
        }
    }
}

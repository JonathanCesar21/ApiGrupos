using System.Data;
using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Controllers;

[ApiController]
[Route("api/contasreceberterceiros")]
[Route("api/contas-receber-terceiros")]
public class ContasReceberTerceirosController : ControllerBase
{
    private const int CommandTimeoutSeconds = 60;
    private const int MaxPeriodoDias = 730;
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ContasReceberTerceirosController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet("titulos")]
    public async Task<ActionResult> GetTitulos(
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] string? tipoData = null,
        [FromQuery] int? codCli = null,
        [FromQuery] int? loja = null,
        [FromQuery] int? codCobranca = null,
        [FromQuery] int? status = null,
        [FromQuery] bool somenteEmAberto = true,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveFiltros(dataInicio, dataFim, tipoData, codCli, out var filtros, out var error))
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

            var titulos = new List<ContaReceberTerceiroTitulo>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var where = BuildWhere(filtros, codCli, loja, codCobranca, status, somenteEmAberto);

            var sql = $"""
                SELECT
                    c.FormaPagto AS CodFormaPagto,
                    fp.Forma AS FormaPagamento,
                    c.Vencimento AS DtVencimento,
                    c.Valor,
                    c.NParcelas,
                    c.Pedido,
                    c.DtPedido,
                    c.Cadastro,
                    c.Observacao,
                    c.Parcela,
                    c.Codcli AS CodCli,
                    cli.nome AS NomeCliente,
                    cli.Bairro,
                    cid.Cidade AS NomeCidade,
                    cli.Limite,
                    cli.Renda,
                    cli.Loja AS LojaCadastro,
                    cli.Fone AS FoneCadastro,
                    cli.FoneRef1 AS FoneReferencia1,
                    cli.FoneRef2 AS FoneReferencia2,
                    c.Seq,
                    c.LoteCobranca,
                    c.Empresa AS Loja,
                    c.CkSPC,
                    c.Sel,
                    c.CodCobranca,
                    c.DtEnvio,
                    c.DtRetorno,
                    c.[Status],
                    c.CCheque,
                    c.Emitente,
                    c.Recebimento,
                    c.Banco,
                    c.Cheque,
                    c.Conta,
                    c.Fone,
                    c.Cpf,
                    c.MesAno,
                    c.Agencia,
                    c.DtDevolucao,
                    c.DtRetirada,
                    c.Cliente,
                    c.Creceber AS CReceber,
                    c.seqCheque AS SeqCheque,
                    c.Carteira,
                    c.Acordo,
                    c.DataLimite,
                    c.operador,
                    c.CodCampanha,
                    c.nISPC,
                    c.nRSPC,
                    c.Cartorio,
                    c.IdEmpresa,
                    c.IdFilial,
                    c.TxAntecipacao,
                    c.ContratoServipa,
                    c.dtCadastro
                FROM CReceberCob c
                LEFT JOIN FormaPG fp
                    ON c.FormaPagto = fp.codforma
                LEFT JOIN Clientes cli
                    ON c.Codcli = cli.Codigo
                LEFT JOIN Cidades cid
                    ON cli.Cidade = cid.Seq
                {where}
                ORDER BY c.Vencimento, c.Empresa, cli.nome, c.Pedido, c.Parcela
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            AddCommonParameters(command, filtros, codCli, loja, codCobranca, status);

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                titulos.Add(ReadTitulo(reader));
            }

            return Ok(titulos);
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
                $"Erro ao consultar contas a receber de terceiros no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar contas a receber de terceiros: {ex.Message}");
        }
    }

    [HttpGet("clientes-resumo")]
    public async Task<ActionResult> GetClientesResumo(
        [FromQuery] DateTime? dataInicio,
        [FromQuery] DateTime? dataFim,
        [FromQuery] string? tipoData = null,
        [FromQuery] int? codCli = null,
        [FromQuery] int? loja = null,
        [FromQuery] int? codCobranca = null,
        [FromQuery] int? status = null,
        [FromQuery] bool somenteEmAberto = true,
        CancellationToken cancellationToken = default)
    {
        if (!TryResolveFiltros(dataInicio, dataFim, tipoData, codCli, out var filtros, out var error))
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

            var clientes = new List<ContaReceberTerceiroClienteResumo>();

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var where = BuildWhere(filtros, codCli, loja, codCobranca, status, somenteEmAberto);

            var sql = $"""
                SELECT
                    c.Codcli AS CodCli,
                    cli.nome AS NomeCliente,
                    cli.Bairro,
                    cid.Cidade AS NomeCidade,
                    cli.Limite,
                    cli.Renda,
                    cli.Loja AS LojaCadastro,
                    COALESCE(NULLIF(cli.Fone, ''), MAX(c.Fone)) AS Fone,
                    cli.FoneRef1 AS FoneReferencia1,
                    cli.FoneRef2 AS FoneReferencia2,
                    MAX(c.Cpf) AS Cpf,
                    c.CodCobranca,
                    MIN(c.Empresa) AS LojaPrincipal,
                    COUNT(DISTINCT c.Empresa) AS QtdeLojas,
                    COUNT(1) AS QtdeTitulos,
                    COUNT(DISTINCT c.Pedido) AS QtdePedidos,
                    SUM(ISNULL(c.Valor, 0)) AS ValorTotalSemJuros,
                    ISNULL(cli.Limite, 0) - SUM(ISNULL(c.Valor, 0)) AS LimiteDisponivel,
                    MIN(c.Vencimento) AS PrimeiroVencimento,
                    MAX(c.Vencimento) AS UltimoVencimento,
                    MIN(c.DtEnvio) AS PrimeiroEnvio,
                    MAX(c.DtEnvio) AS UltimoEnvio,
                    MAX(c.DtPedido) AS UltimaCompra,
                    MIN(c.DataLimite) AS ProximaDataLimite,
                    MAX(c.DtRetorno) AS UltimoRetorno,
                    SUM(CASE WHEN c.Recebimento IS NULL THEN 0 ELSE 1 END) AS QtdeComRecebimento,
                    SUM(CASE WHEN c.DtRetorno IS NULL THEN 0 ELSE 1 END) AS QtdeComRetorno
                FROM CReceberCob c
                LEFT JOIN Clientes cli
                    ON c.Codcli = cli.Codigo
                LEFT JOIN Cidades cid
                    ON cli.Cidade = cid.Seq
                {where}
                GROUP BY
                    c.Codcli,
                    cli.nome,
                    cli.Bairro,
                    cid.Cidade,
                    cli.Limite,
                    cli.Renda,
                    cli.Loja,
                    cli.Fone,
                    cli.FoneRef1,
                    cli.FoneRef2,
                    c.CodCobranca
                ORDER BY
                    SUM(ISNULL(c.Valor, 0)) DESC,
                    MIN(c.Vencimento),
                    cli.nome
                """;

            await using var command = new SqlCommand(sql, connection)
            {
                CommandTimeout = CommandTimeoutSeconds
            };

            AddCommonParameters(command, filtros, codCli, loja, codCobranca, status);

            await using var reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                clientes.Add(ReadClienteResumo(reader));
            }

            return Ok(clientes);
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
                $"Erro ao consultar resumo de contas a receber de terceiros no banco de dados: {ex.Message}");
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                $"Erro inesperado ao consultar resumo de contas a receber de terceiros: {ex.Message}");
        }
    }

    private static bool TryResolveFiltros(
        DateTime? dataInicio,
        DateTime? dataFim,
        string? tipoData,
        int? codCli,
        out TerceirosFiltros filtros,
        out string error)
    {
        filtros = new TerceirosFiltros();
        error = string.Empty;

        if (!TryResolveDataColumn(tipoData, out var dateColumn, out error))
        {
            return false;
        }

        filtros.DateColumn = dateColumn;
        filtros.HasPeriodo = dataInicio.HasValue || dataFim.HasValue;

        if (!filtros.HasPeriodo)
        {
            if (!codCli.HasValue)
            {
                error = "Informe 'dataInicio' e 'dataFim', ou informe 'codCli' para consultar um cliente especifico.";
                return false;
            }

            return true;
        }

        if (!dataInicio.HasValue || !dataFim.HasValue)
        {
            error = "Os parametros 'dataInicio' e 'dataFim' devem ser informados juntos.";
            return false;
        }

        filtros.Inicio = dataInicio.Value.Date;
        var fim = dataFim.Value.Date;

        if (fim < filtros.Inicio)
        {
            error = "O parametro 'dataFim' deve ser maior ou igual a 'dataInicio'.";
            return false;
        }

        if (fim == DateTime.MaxValue.Date)
        {
            error = "O parametro 'dataFim' excede o limite suportado.";
            return false;
        }

        var periodoDias = (fim - filtros.Inicio).TotalDays + 1;
        if (periodoDias > MaxPeriodoDias)
        {
            error = $"O periodo maximo permitido para contas a receber de terceiros e de {MaxPeriodoDias} dias.";
            return false;
        }

        filtros.FimExclusivo = fim.AddDays(1);
        return true;
    }

    private static bool TryResolveDataColumn(string? tipoData, out string dateColumn, out string error)
    {
        var key = string.IsNullOrWhiteSpace(tipoData)
            ? "vencimento"
            : tipoData.Trim().ToLowerInvariant();

        dateColumn = key switch
        {
            "vencimento" => "c.Vencimento",
            "envio" => "c.DtEnvio",
            "retorno" => "c.DtRetorno",
            "pedido" => "c.DtPedido",
            "cadastro" => "c.Cadastro",
            "dtcadastro" => "c.dtCadastro",
            "cadastro-terceiros" => "c.dtCadastro",
            "data-limite" => "c.DataLimite",
            "limite" => "c.DataLimite",
            "devolucao" => "c.DtDevolucao",
            "retirada" => "c.DtRetirada",
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(dateColumn))
        {
            error = string.Empty;
            return true;
        }

        error = "O parametro 'tipoData' deve ser um destes valores: vencimento, envio, retorno, pedido, cadastro, dtcadastro, cadastro-terceiros, data-limite, limite, devolucao ou retirada.";
        return false;
    }

    private static string BuildWhere(
        TerceirosFiltros filtros,
        int? codCli,
        int? loja,
        int? codCobranca,
        int? status,
        bool somenteEmAberto)
    {
        var where = new List<string>();

        if (filtros.HasPeriodo)
        {
            where.Add($"{filtros.DateColumn} >= @DataInicio AND {filtros.DateColumn} < @DataFimExclusivo");
        }

        if (codCli.HasValue)
        {
            where.Add("c.Codcli = @CodCli");
        }

        if (loja.HasValue)
        {
            where.Add("c.Empresa = @Loja");
        }

        if (codCobranca.HasValue)
        {
            where.Add("c.CodCobranca = @CodCobranca");
        }

        if (status.HasValue)
        {
            where.Add("c.[Status] = @Status");
        }

        if (somenteEmAberto)
        {
            where.Add("c.Recebimento IS NULL");
        }

        return where.Count == 0
            ? string.Empty
            : "WHERE " + string.Join(" AND ", where);
    }

    private static void AddCommonParameters(
        SqlCommand command,
        TerceirosFiltros filtros,
        int? codCli,
        int? loja,
        int? codCobranca,
        int? status)
    {
        if (filtros.HasPeriodo)
        {
            command.Parameters.Add("@DataInicio", SqlDbType.DateTime).Value = filtros.Inicio;
            command.Parameters.Add("@DataFimExclusivo", SqlDbType.DateTime).Value = filtros.FimExclusivo;
        }

        if (codCli.HasValue)
        {
            command.Parameters.Add("@CodCli", SqlDbType.Int).Value = codCli.Value;
        }

        if (loja.HasValue)
        {
            command.Parameters.Add("@Loja", SqlDbType.Int).Value = loja.Value;
        }

        if (codCobranca.HasValue)
        {
            command.Parameters.Add("@CodCobranca", SqlDbType.Int).Value = codCobranca.Value;
        }

        if (status.HasValue)
        {
            command.Parameters.Add("@Status", SqlDbType.Int).Value = status.Value;
        }
    }

    private static ContaReceberTerceiroTitulo ReadTitulo(SqlDataReader reader)
    {
        return new ContaReceberTerceiroTitulo
        {
            CodFormaPagto = ReadInt32(reader, 0),
            FormaPagamento = ReadString(reader, 1),
            DtVencimento = ReadDateTime(reader, 2),
            Valor = reader.IsDBNull(3) ? 0 : Convert.ToDecimal(reader.GetValue(3)),
            NParcelas = ReadInt32(reader, 4),
            Pedido = ReadString(reader, 5),
            DtPedido = ReadDateTime(reader, 6),
            Cadastro = ReadDateTime(reader, 7),
            Observacao = ReadString(reader, 8),
            Parcela = ReadInt32(reader, 9),
            CodCli = Convert.ToInt32(reader.GetValue(10)),
            NomeCliente = reader.IsDBNull(11) ? string.Empty : reader.GetString(11),
            Bairro = ReadString(reader, 12),
            NomeCidade = ReadString(reader, 13),
            Limite = ReadDecimal(reader, 14),
            Renda = ReadDecimal(reader, 15),
            LojaCadastro = ReadInt32(reader, 16),
            FoneCadastro = ReadString(reader, 17),
            FoneReferencia1 = ReadString(reader, 18),
            FoneReferencia2 = ReadString(reader, 19),
            Seq = ReadInt64(reader, 20),
            LoteCobranca = ReadString(reader, 21),
            Loja = ReadInt32(reader, 22),
            CkSpc = ReadInt32(reader, 23),
            Sel = ReadInt32(reader, 24),
            CodCobranca = ReadInt32(reader, 25),
            DtEnvio = ReadDateTime(reader, 26),
            DtRetorno = ReadDateTime(reader, 27),
            Status = ReadInt32(reader, 28),
            CCheque = ReadInt32(reader, 29),
            Emitente = ReadString(reader, 30),
            Recebimento = ReadDateTime(reader, 31),
            Banco = ReadString(reader, 32),
            Cheque = ReadString(reader, 33),
            Conta = ReadString(reader, 34),
            Fone = ReadString(reader, 35),
            Cpf = ReadString(reader, 36),
            MesAno = ReadString(reader, 37),
            Agencia = ReadString(reader, 38),
            DtDevolucao = ReadDateTime(reader, 39),
            DtRetirada = ReadDateTime(reader, 40),
            Cliente = ReadInt32(reader, 41),
            CReceber = ReadInt64(reader, 42),
            SeqCheque = ReadInt64(reader, 43),
            Carteira = ReadString(reader, 44),
            Acordo = ReadString(reader, 45),
            DataLimite = ReadDateTime(reader, 46),
            Operador = ReadString(reader, 47),
            CodCampanha = ReadInt32(reader, 48),
            NIspc = ReadString(reader, 49),
            NRspc = ReadString(reader, 50),
            Cartorio = ReadString(reader, 51),
            IdEmpresa = ReadInt32(reader, 52),
            IdFilial = ReadInt32(reader, 53),
            TxAntecipacao = ReadDecimal(reader, 54),
            ContratoServipa = ReadString(reader, 55),
            DtCadastro = ReadDateTime(reader, 56)
        };
    }

    private static ContaReceberTerceiroClienteResumo ReadClienteResumo(SqlDataReader reader)
    {
        return new ContaReceberTerceiroClienteResumo
        {
            CodCli = Convert.ToInt32(reader.GetValue(0)),
            NomeCliente = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
            Bairro = ReadString(reader, 2),
            NomeCidade = ReadString(reader, 3),
            Limite = ReadDecimal(reader, 4),
            Renda = ReadDecimal(reader, 5),
            LojaCadastro = ReadInt32(reader, 6),
            Fone = ReadString(reader, 7),
            FoneReferencia1 = ReadString(reader, 8),
            FoneReferencia2 = ReadString(reader, 9),
            Cpf = ReadString(reader, 10),
            CodCobranca = ReadInt32(reader, 11),
            LojaPrincipal = ReadInt32(reader, 12),
            QtdeLojas = Convert.ToInt32(reader.GetValue(13)),
            QtdeTitulos = Convert.ToInt32(reader.GetValue(14)),
            QtdePedidos = Convert.ToInt32(reader.GetValue(15)),
            ValorTotalSemJuros = reader.IsDBNull(16) ? 0 : Convert.ToDecimal(reader.GetValue(16)),
            LimiteDisponivel = ReadDecimal(reader, 17),
            PrimeiroVencimento = ReadDateTime(reader, 18),
            UltimoVencimento = ReadDateTime(reader, 19),
            PrimeiroEnvio = ReadDateTime(reader, 20),
            UltimoEnvio = ReadDateTime(reader, 21),
            UltimaCompra = ReadDateTime(reader, 22),
            ProximaDataLimite = ReadDateTime(reader, 23),
            UltimoRetorno = ReadDateTime(reader, 24),
            QtdeComRecebimento = Convert.ToInt32(reader.GetValue(25)),
            QtdeComRetorno = Convert.ToInt32(reader.GetValue(26))
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

    private static long? ReadInt64(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static decimal? ReadDecimal(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static DateTime? ReadDateTime(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Convert.ToDateTime(reader.GetValue(ordinal));
    }

    private sealed class TerceirosFiltros
    {
        public bool HasPeriodo { get; set; }

        public DateTime Inicio { get; set; }

        public DateTime FimExclusivo { get; set; }

        public string DateColumn { get; set; } = string.Empty;
    }
}

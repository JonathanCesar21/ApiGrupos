using System.Data;
using ApiGrupos.Models;
using Microsoft.Data.SqlClient;

namespace ApiGrupos.Services;

// Apenas fragmentos definidos no servidor entram no SQL. Valores usam parametros tipados.
public sealed class DashboardCommand(string sql)
{
    public string Sql { get; set; } = sql;
    public List<SqlParameter> Parameters { get; } = [];
    public void Add(string name, SqlDbType type, object value, int size = 0)
    {
        var parameter = new SqlParameter(name, type) { Value = value };
        if (size > 0) parameter.Size = size;
        Parameters.Add(parameter);
    }
}

public static class DashboardSql
{
    public const int TimeoutSeconds = 60;
    public const int MaxPeriodoDias = 120;

    public static DashboardCommand Recebimentos(DashboardRecebimentosQuery q)
    {
        var column = (q.TipoData ?? "baixa").ToLowerInvariant() switch
        {
            "baixa" => "c.Baixa", "vencimento" => "c.Vencimento",
            _ => throw new ArgumentException("tipoData deve ser baixa ou vencimento.")
        };
        var cmd = new DashboardCommand("""
            SELECT c.FormaPagto AS CodFormaPagamento, fp.Forma AS FormaPagamento,
                   fp.Tipo, c.Operador, c.Valor, c.vlpago, c.Baixa,
                   c.Empresa AS Loja, c.EmpresaRec AS LojaRecebimento, c.Vencimento, c.Pedido
            FROM ContaReceber c
            INNER JOIN FormaPG fp ON c.FormaPagto = fp.codforma
            WHERE c.Baixa >= '20220101'
            """);
        Periodo(cmd, q, column, required: true);
        Loja(cmd, "c.Empresa", q.Loja);
        Loja(cmd, "c.EmpresaRec", q.LojaRecebimento, "@LojaRecebimento");
        cmd.Sql += " ORDER BY c.Baixa, c.Empresa, c.EmpresaRec, c.Pedido";
        return cmd;
    }

    public static DashboardCommand Titulos(DashboardTitulosQuery q)
    {
        var column = (q.TipoData ?? "pedido").ToLowerInvariant() switch
        {
            "pedido" => "c.DtPedido", "vencimento" => "c.Vencimento", "baixa" => "c.Baixa",
            _ => throw new ArgumentException("tipoData deve ser pedido, vencimento ou baixa.")
        };
        var situation = (q.Situacao ?? "todos").ToLowerInvariant() switch
        {
            "todos" => "", "baixa-nula" => " AND c.Baixa IS NULL",
            "pago-nulo" => " AND c.Pago IS NULL", "pago-preenchido" => " AND c.Pago IS NOT NULL",
            _ => throw new ArgumentException("situacao deve ser todos, baixa-nula, pago-nulo ou pago-preenchido.")
        };
        var cmd = new DashboardCommand("""
            SELECT c.CodCli, c.Pedido, c.Parcela, c.NParcelas, c.Empresa AS Loja,
                   c.Valor, c.Vencimento AS DtVencimento, c.DtPedido, c.Pago,
                   c.Baixa AS DtBaixa, c.FormaPagto AS CodFormaPgt, fp.Forma AS FormaPagamento
            FROM CReceber c
            INNER JOIN FormaPG fp ON c.FormaPagto = fp.codforma
            WHERE 1 = 1
            """ + situation);
        Periodo(cmd, q, column, required: false);
        Loja(cmd, "c.Empresa", q.Loja);
        cmd.Sql += " ORDER BY c.Vencimento, c.Empresa, c.Pedido, c.Parcela";
        return cmd;
    }

    public static DashboardCommand MetasLojas(DashboardMetasQuery q)
    {
        var cmd = new DashboardCommand("""
            SELECT m.Loja, m.Ano, m.Mes,
                   DATEADD(month, m.Mes - 1, DATEADD(year, m.Ano - 1900, 0)) AS Data,
                   m.Vendas, m.Avista, m.Receber, m.Terceiros, m.ValorRM
            FROM Metas m WHERE m.Ano >= 2022
            """);
        Metas(cmd, q, "m");
        cmd.Sql += " ORDER BY m.Ano, m.Mes, m.Loja";
        return cmd;
    }

    public static DashboardCommand MetasVendedores(DashboardMetasQuery q)
    {
        var cmd = new DashboardCommand("""
            SELECT mv.Loja, v.codVend, v.vendedor, mv.Ano, mv.Mes,
                   DATEADD(month, mv.Mes - 1, DATEADD(year, mv.Ano - 1900, 0)) AS Data, mv.Vendas
            FROM Vendedores v
            INNER JOIN MetasVendedor mv ON v.codvend = mv.CodVend
            WHERE mv.Ano >= 2022
            """);
        Metas(cmd, q, "mv");
        cmd.Sql += " ORDER BY mv.Ano, mv.Mes, mv.Loja, v.codVend";
        return cmd;
    }

    public static DashboardCommand Vendedores(DashboardVendedoresQuery q)
    {
        var cmd = new DashboardCommand("SELECT v.codVend, v.vendedor, v.Loja FROM Vendedores v WHERE 1 = 1");
        Loja(cmd, "v.Loja", q.Loja);
        if (q.CodVend is not null)
        {
            if (q.CodVend.Length is 0 or > 100) throw new ArgumentException("codVend deve ter entre 1 e 100 caracteres.");
            cmd.Sql += " AND v.codVend = @CodVend";
            cmd.Add("@CodVend", SqlDbType.VarChar, q.CodVend, 100);
        }
        cmd.Sql += " ORDER BY v.codVend, v.Loja";
        return cmd;
    }

    public static DashboardCommand EntradasConfirmar(DashboardPeriodoQuery q)
    {
        var cmd = new DashboardCommand("""
            SELECT ec.Loja, ec.Quant, ec.Barras, ec.NotaFiscal, ec.Data,
                   pb.Valor, pb.Barras AS BarrasProduto, pb.Grupo, pb.SubGrupo,
                   pb.Fornecedor, pb.Referencia, pb.Secao, pb.Numero, pb.Cor
            FROM EntradaControle ec
            INNER JOIN ProdutoBarras pb ON pb.Barras = ec.Barras
            WHERE 1 = 1
            """);
        Periodo(cmd, q, "ec.Data", required: false, maxDays: null);
        Loja(cmd, "ec.Loja", q.Loja);
        cmd.Sql += " ORDER BY ec.Loja, ec.Data, ec.NotaFiscal, ec.Barras";
        return cmd;
    }

    public static DashboardCommand Transferencias(DashboardTransferenciasQuery q)
    {
        var cmd = new DashboardCommand(TransferenciasBase + """
             SELECT Referencia, CodSecao, DescSecao, CodSubGrupo, DescSubGrupo,
                    CodCor, DescCor, CodNumero, DescNumero, CodFor, NomeFornecedor,
                    Quant, NotaFiscal, Loja, Origem, StatusImpressao, DataTransferencia,
                    DataAuto, UsuarioAuto, UsuarioConfirma, Automatica, TipoTransferencia, OrigemRegistro, Grupo
             FROM Transferencias WHERE 1 = 1
            """);
        Periodo(cmd, q, "DataTransferencia", required: false, maxDays: null);
        Loja(cmd, "Loja", q.Loja);
        Loja(cmd, "Origem", q.Origem, "@Origem");
        if (q.TipoTransferencia is not null)
        {
            var tipo = q.TipoTransferencia.ToUpperInvariant();
            if (!new[] { "AUTOMATICA AUTORIZADA", "AUTOMATICA PENDENTE", "MANUAL", "MANUAL EFETIVADA", "MANUAL PENDENTE" }.Contains(tipo))
                throw new ArgumentException("tipoTransferencia invalido.");
            cmd.Sql += " AND TipoTransferencia = @TipoTransferencia";
            cmd.Add("@TipoTransferencia", SqlDbType.VarChar, tipo, 30);
        }
        if (q.SomentePendentes) cmd.Sql += " AND TipoTransferencia IN ('AUTOMATICA PENDENTE', 'MANUAL PENDENTE')";
        cmd.Sql += " ORDER BY DataTransferencia, OrigemRegistro, Loja, NotaFiscal, Referencia";
        return cmd;
    }

    public const string TransferenciasBase = """
        WITH Transferencias AS
        (
            SELECT t.Referencia, p.Secao AS CodSecao, s.Descricao AS DescSecao,
                   p.SubGrupo AS CodSubGrupo, sg.nomesubgrupo AS DescSubGrupo,
                   t.codcor AS CodCor, c.Descricao AS DescCor,
                   t.CodNumero, n.Descricao AS DescNumero, t.CodFor, f.nome_for AS NomeFornecedor,
                   t.Quant, t.NotaFiscal, t.Destino AS Loja, t.Origem,
                   t.statusimpressao AS StatusImpressao, t.Data AS DataTransferencia,
                   t.DataAuto, t.UsuarioAuto, t.UsuarioConfirma, t.Automatica,
                   CASE WHEN t.Automatica = 1 AND t.UsuarioAuto IS NOT NULL THEN 'AUTOMATICA AUTORIZADA'
                        WHEN t.Automatica = 1 AND t.UsuarioAuto IS NULL THEN 'AUTOMATICA PENDENTE'
                        ELSE 'MANUAL' END AS TipoTransferencia,
                   'TRANSFERENCIA' AS OrigemRegistro, g.nomegrupo AS Grupo
            FROM Transferencia t
            INNER JOIN Produtos p ON p.referencia = t.referencia
            LEFT JOIN Secao s ON s.Codigo = p.secao
            LEFT JOIN SubGrupos sg ON sg.codsubgrupo = p.subgrupo
            LEFT JOIN Grupos g ON g.codgrupo = p.Grupo
            LEFT JOIN Cor c ON c.Codigo = t.codcor
            LEFT JOIN Numeracao n ON n.Codigo = t.CodNumero
            LEFT JOIN Fornecedor f ON f.cod_for = t.CodFor
            WHERE t.Data >= '20240101'
            UNION ALL
            SELECT tc.Referencia, p.Secao, s.Descricao, p.SubGrupo, sg.nomesubgrupo,
                   tc.codcor, c.Descricao, tc.CodNumero, n.Descricao, tc.CodFor, f.nome_for,
                   tc.Quant, tc.NotaFiscal, tc.Destino, tc.Origem,
                   NULL, tc.Data, NULL, t.UsuarioAuto, t.UsuarioConfirma, 0,
                   CASE WHEN t.UsuarioConfirma IS NOT NULL THEN 'MANUAL EFETIVADA'
                        ELSE 'MANUAL PENDENTE' END, 'CONTROLE', g.nomegrupo
            FROM TransferenciaControle tc
            LEFT JOIN Transferencia t ON t.referencia = tc.referencia
                 AND t.codcor = tc.codcor AND t.CodNumero = tc.CodNumero
                 AND t.NotaFiscal = tc.NotaFiscal AND t.Origem = tc.Origem AND t.Destino = tc.Destino
            INNER JOIN Produtos p ON p.referencia = tc.referencia
            LEFT JOIN Secao s ON s.Codigo = p.secao
            LEFT JOIN SubGrupos sg ON sg.codsubgrupo = p.subgrupo
            LEFT JOIN Grupos g ON g.codgrupo = p.Grupo
            LEFT JOIN Cor c ON c.Codigo = tc.codcor
            LEFT JOIN Numeracao n ON n.Codigo = tc.CodNumero
            LEFT JOIN Fornecedor f ON f.cod_for = tc.CodFor
            WHERE tc.Origem = 5 AND tc.Data >= '20260101'
        )
        """;

    public static DashboardCommand Ativacoes(DashboardPeriodoQuery q)
    {
        var cmd = new DashboardCommand("""
            WITH PrimeiraCompra AS
            (
                SELECT codcli, MIN(data) AS Data FROM ConsultaVenda GROUP BY codcli
            )
            SELECT c.codigo AS CodCli, MIN(cv.data) AS DtPrimeiraCompra, cv.Loja,
                   cv.total AS Valor, cv.Pedido, c.Cadastro AS DtCadastro, cv.Vendedor
            FROM Clientes c
            INNER JOIN ConsultaVenda cv ON c.codigo = cv.codcli
            INNER JOIN PrimeiraCompra pc ON cv.codcli = pc.codcli AND cv.data = pc.Data
            WHERE c.codigo <> 1376401
            """);
        Periodo(cmd, q, "cv.data", required: true);
        Loja(cmd, "cv.Loja", q.Loja);
        cmd.Sql += " GROUP BY c.codigo, c.nome, cv.Loja, cv.total, cv.Pedido, c.Cadastro, cv.Vendedor ORDER BY cv.Pedido, c.codigo";
        return cmd;
    }

    public static DashboardCommand Referencias(DashboardReferenciasQuery q)
    {
        var cmd = new DashboardCommand("""
            WITH Referencias AS
            (
                SELECT p.referencia, p.Custo, p.Secao AS CodSecao, s.Descricao, p.codigo AS CodProd,
                       f.nome_for AS Fornecedor, g.nomegrupo AS Grupo, sb.nomesubgrupo AS SubGrupo,
                       p.Categoria AS CodCategoria, c.nome AS Categoria,
                       ROW_NUMBER() OVER (PARTITION BY p.referencia ORDER BY p.codigo) AS rn
                FROM Produtos p
                INNER JOIN Grupos g ON p.Grupo = g.codgrupo
                INNER JOIN Secao s ON p.Secao = s.Codigo
                INNER JOIN SubGrupos sb ON p.subgrupo = sb.codsubgrupo
                INNER JOIN Fornecedor f ON p.fornecedor = f.cod_for
                LEFT JOIN Categoria c ON p.Categoria = c.codigo
            )
            SELECT Referencia, Custo, CodSecao, Descricao, CodProd, Fornecedor, Grupo, SubGrupo, CodCategoria, Categoria
            FROM Referencias WHERE rn = 1
            """);
        if (q.Referencia is not null)
        {
            if (q.Referencia.Length is 0 or > 200) throw new ArgumentException("referencia deve ter entre 1 e 200 caracteres.");
            cmd.Sql += " AND Referencia = @Referencia";
            cmd.Add("@Referencia", SqlDbType.VarChar, q.Referencia, 200);
        }
        cmd.Sql += " ORDER BY Referencia";
        return cmd;
    }

    public static DashboardCommand Terceiros(DashboardTerceirosQuery q)
    {
        var cmd = new DashboardCommand("""
            SELECT cr.Codcli, cr.Vencimento, cr.Valor, cr.Pedido, cr.DtPedido, cr.Parcela,
                   cr.NParcelas, cr.CodCobranca, ct.Nome AS NomeCobranca, cr.Empresa AS Loja, cr.Status
            FROM CReceberCob cr
            INNER JOIN CobrancaTerceiros ct ON cr.CodCobranca = ct.Codigo
            WHERE 1 = 1
            """);
        Periodo(cmd, q, "cr.Vencimento", required: true);
        Loja(cmd, "cr.Empresa", q.Loja);
        if (q.Status.HasValue)
        {
            cmd.Sql += " AND cr.Status = @Status";
            cmd.Add("@Status", SqlDbType.Int, q.Status.Value);
        }
        cmd.Sql += " ORDER BY cr.Vencimento, cr.Empresa, cr.Pedido, cr.Parcela";
        return cmd;
    }

    // DATEADD/DATEDIFF preservam compatibilidade com SQL Server 2005/2008.
    public static DashboardCommand VendasAgregado(DashboardPeriodoQuery q)
    {
        var cmd = new DashboardCommand("""
            SELECT DATEADD(day, DATEDIFF(day, 0, data), 0) AS DataVenda,
                   Referencia, Numero, Loja, Grupo, SubGrupo, Fornecedor,
                   SUM(quant) AS Quantidade, SUM(ValorTotal) AS FaturamentoPbix,
                   SUM(quant * ValorCusto) AS CustoTotalPbix
            FROM ConsultaVenda WHERE 1 = 1
            """);
        Periodo(cmd, q, "data", required: true);
        Loja(cmd, "Loja", q.Loja);
        cmd.Sql += " GROUP BY DATEADD(day, DATEDIFF(day, 0, data), 0), Referencia, Numero, Loja, Grupo, SubGrupo, Fornecedor";
        return cmd;
    }

    public const string EntradasBase = """
        WITH Entradas AS
        (
            SELECT c.Data, c.Referencia, c.Loja, c.Quant, c.Valor, p.Venda,
                   g.nomegrupo AS Grupo, sg.nomesubgrupo AS SubGrupo, f.nome_for AS Fornecedor,
                   n.Descricao AS Numero,
                   ROW_NUMBER() OVER
                   (
                       PARTITION BY c.NotaFiscal, c.Referencia, c.Loja, n.Descricao, f.nome_for,
                                    p.subgrupo, p.Venda, sg.nomesubgrupo, g.nomegrupo,
                                    p.Colecao, p.Categoria, c.barras
                       ORDER BY c.Data DESC
                   ) AS rn
            FROM centrada c
            INNER JOIN Numeracao n ON c.CodNumero = n.codigo
            INNER JOIN Fornecedor f ON c.codfor = f.cod_for
            INNER JOIN Produtos p ON c.CodProd = p.codigo
            INNER JOIN SubGrupos sg ON p.subgrupo = sg.codsubgrupo
            INNER JOIN Grupos g ON p.Grupo = g.codgrupo
            WHERE c.Data >= '20220101'
        )
        """;

    public static DashboardCommand EntradasAgregado(DashboardPeriodoQuery q)
    {
        var cmd = new DashboardCommand(EntradasBase + """
             SELECT DATEADD(day, DATEDIFF(day, 0, Data), 0) AS DataEntrada,
                    Referencia, Numero, Loja, Grupo, SubGrupo, Fornecedor,
                    SUM(Quant) AS Quantidade, SUM(Quant * Venda) AS ValorVendaEntradaPbix,
                    SUM(Quant * Valor) AS CustoTotalEntradaPbix
             FROM Entradas WHERE rn = 1
            """);
        Periodo(cmd, q, "Data", required: true);
        Loja(cmd, "Loja", q.Loja);
        cmd.Sql += " GROUP BY DATEADD(day, DATEDIFF(day, 0, Data), 0), Referencia, Numero, Loja, Grupo, SubGrupo, Fornecedor";
        return cmd;
    }

    private static void Metas(DashboardCommand cmd, DashboardMetasQuery q, string alias)
    {
        if (q.Ano is < 2022 or > 9999 || q.Mes is < 1 or > 12) throw new ArgumentException("ano deve ser 2022..9999 e mes deve ser 1..12.");
        if (q.Ano.HasValue) { cmd.Sql += $" AND {alias}.Ano = @Ano"; cmd.Add("@Ano", SqlDbType.Int, q.Ano.Value); }
        if (q.Mes.HasValue) { cmd.Sql += $" AND {alias}.Mes = @Mes"; cmd.Add("@Mes", SqlDbType.Int, q.Mes.Value); }
        Loja(cmd, alias + ".Loja", q.Loja);
    }

    private static void Loja(DashboardCommand cmd, string column, int? value, string parameter = "@Loja")
    {
        if (!value.HasValue) return;
        if (value < 0) throw new ArgumentException("Codigo de loja/origem nao pode ser negativo.");
        cmd.Sql += $" AND {column} = {parameter}";
        cmd.Add(parameter, SqlDbType.Int, value.Value);
    }

    private static void Periodo(DashboardCommand cmd, DashboardPeriodoQuery q, string column, bool required, int? maxDays = MaxPeriodoDias)
    {
        if (!required && q.DataInicio is null && q.DataFim is null) return;
        if (q.DataInicio is null || q.DataFim is null) throw new ArgumentException("dataInicio e dataFim devem ser informadas juntas.");
        var inicio = q.DataInicio.Value.Date;
        var fim = q.DataFim.Value.Date;
        if (inicio < new DateTime(1753, 1, 1)) throw new ArgumentException("dataInicio deve ser a partir de 1753-01-01.");
        if (fim < inicio) throw new ArgumentException("dataFim deve ser maior ou igual a dataInicio.");
        if (fim == DateTime.MaxValue.Date) throw new ArgumentException("dataFim excede o limite suportado (9999-12-30).");
        if (maxDays.HasValue && (fim - inicio).Days + 1 > maxDays.Value) throw new ArgumentException($"O periodo maximo e de {maxDays} dias inclusivos.");
        cmd.Sql += $" AND {column} >= @DataInicio AND {column} < @DataFimExclusivo";
        cmd.Add("@DataInicio", SqlDbType.DateTime, inicio);
        cmd.Add("@DataFimExclusivo", SqlDbType.DateTime, fim.AddDays(1));
    }
}

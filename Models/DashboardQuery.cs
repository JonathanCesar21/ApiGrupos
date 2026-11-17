using System.ComponentModel.DataAnnotations;

namespace ApiGrupos.Models;

public class DashboardPeriodoQuery
{
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    [Range(0, int.MaxValue)] public int? Loja { get; set; }
}

public class DashboardRecebimentosQuery : DashboardPeriodoQuery
{
    public string TipoData { get; set; } = "baixa";
    [Range(0, int.MaxValue)] public int? LojaRecebimento { get; set; }
}

public class DashboardTitulosQuery : DashboardPeriodoQuery
{
    public string TipoData { get; set; } = "pedido";
    public string Situacao { get; set; } = "todos";
}

public class DashboardMetasQuery
{
    [Range(2022, 9999)] public int? Ano { get; set; }
    [Range(1, 12)] public int? Mes { get; set; }
    [Range(0, int.MaxValue)] public int? Loja { get; set; }
}

public class DashboardVendedoresQuery
{
    [Range(0, int.MaxValue)] public int? Loja { get; set; }
    [StringLength(100, MinimumLength = 1)] public string? CodVend { get; set; }
}

public class DashboardTransferenciasQuery : DashboardPeriodoQuery
{
    [Range(0, int.MaxValue)] public int? Origem { get; set; }
    public string? TipoTransferencia { get; set; }
    public bool SomentePendentes { get; set; }
}

public class DashboardReferenciasQuery
{
    [StringLength(200)] public string? Referencia { get; set; }
}

public class DashboardTerceirosQuery : DashboardPeriodoQuery
{
    public int? Status { get; set; }
}

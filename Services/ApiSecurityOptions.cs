namespace ApiGrupos.Services;

public class ApiSecurityOptions
{
    public const string SectionName = "ApiSecurity";

    public string ApiKeyHeaderName { get; set; } = "X-API-Key";

    public string ApiKeyHash { get; set; } = string.Empty;

    public string AdminUsername { get; set; } = string.Empty;

    public string AdminPasswordHash { get; set; } = string.Empty;
}

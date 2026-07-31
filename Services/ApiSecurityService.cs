using Microsoft.Extensions.Options;

namespace ApiGrupos.Services;

public class ApiSecurityService
{
    private readonly ApiSecurityOptions _options;

    public ApiSecurityService(IOptions<ApiSecurityOptions> options)
    {
        _options = options.Value;
    }

    public string ApiKeyHeaderName =>
        string.IsNullOrWhiteSpace(_options.ApiKeyHeaderName)
            ? "X-API-Key"
            : _options.ApiKeyHeaderName.Trim();

    public bool IsApiKeyConfigured => SecretHash.IsSha256Hash(_options.ApiKeyHash);

    public bool IsAdminConfigured =>
        !string.IsNullOrWhiteSpace(_options.AdminUsername) &&
        SecretHash.IsSha256Hash(_options.AdminPasswordHash);

    public bool ValidateApiKey(string? apiKey)
    {
        return apiKey is not null && SecretHash.VerifySha256(apiKey, _options.ApiKeyHash);
    }

    public bool ValidateAdminCredentials(string username, string password)
    {
        return IsAdminConfigured &&
            string.Equals(username, _options.AdminUsername, StringComparison.Ordinal) &&
            SecretHash.VerifySha256(password, _options.AdminPasswordHash);
    }
}

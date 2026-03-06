using Microsoft.Data.SqlClient;

namespace ApiGrupos.Services;

public class ConnectionStringProvider
{
    private readonly object _sync = new();
    private readonly SqlConnectionStringBuilder _baseBuilder;
    private string? _currentConnectionString;

    public ConnectionStringProvider(string? baseConnectionString)
    {
        var baseValue = string.IsNullOrWhiteSpace(baseConnectionString)
            ? "Server=tcp:lcsi.nuvem.info,1433;Database=BdRubi;TrustServerCertificate=True;Encrypt=False;"
            : baseConnectionString;

        _baseBuilder = new SqlConnectionStringBuilder(baseValue);

        var hasUser = !string.IsNullOrWhiteSpace(_baseBuilder.UserID) && !_baseBuilder.UserID.Contains("COLOCAR_USUARIO_AQUI", StringComparison.OrdinalIgnoreCase);
        var hasPassword = !string.IsNullOrWhiteSpace(_baseBuilder.Password) && !_baseBuilder.Password.Contains("COLOCAR_SENHA_AQUI", StringComparison.OrdinalIgnoreCase);

        if (hasUser && hasPassword)
        {
            _currentConnectionString = _baseBuilder.ConnectionString;
        }
    }

    public bool IsConfigured
    {
        get
        {
            lock (_sync)
            {
                return !string.IsNullOrWhiteSpace(_currentConnectionString);
            }
        }
    }

    public string? GetConnectionString()
    {
        lock (_sync)
        {
            return _currentConnectionString;
        }
    }

    public void SetCredentials(string userId, string password)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Usuario e senha sao obrigatorios.");
        }

        lock (_sync)
        {
            var builder = new SqlConnectionStringBuilder(_baseBuilder.ConnectionString)
            {
                UserID = userId,
                Password = password
            };

            _currentConnectionString = builder.ConnectionString;
        }
    }
}

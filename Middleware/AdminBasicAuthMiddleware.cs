using System.Net.Http.Headers;
using System.Text;
using ApiGrupos.Services;

namespace ApiGrupos.Middleware;

public class AdminBasicAuthMiddleware
{
    private const string Realm = "ApiGrupos Admin";
    private readonly RequestDelegate _next;

    public AdminBasicAuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApiSecurityService security)
    {
        if (!RequiresAdminAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!security.IsAdminConfigured)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Credenciais de admin nao configuradas.");
            return;
        }

        if (!TryGetBasicCredentials(context.Request.Headers.Authorization, out var username, out var password) ||
            !security.ValidateAdminCredentials(username, password))
        {
            context.Response.Headers.WWWAuthenticate = $"Basic realm=\"{Realm}\", charset=\"UTF-8\"";
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Autenticacao de admin obrigatoria.");
            return;
        }

        await _next(context);
    }

    public static bool RequiresAdminAuthentication(PathString path)
    {
        return path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/configuracao", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/configuracao", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryGetBasicCredentials(
        string? authorizationHeader,
        out string username,
        out string password)
    {
        username = string.Empty;
        password = string.Empty;

        if (string.IsNullOrWhiteSpace(authorizationHeader) ||
            !AuthenticationHeaderValue.TryParse(authorizationHeader, out var header) ||
            !string.Equals(header.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(header.Parameter))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header.Parameter));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return false;
            }

            username = decoded[..separatorIndex];
            password = decoded[(separatorIndex + 1)..];
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

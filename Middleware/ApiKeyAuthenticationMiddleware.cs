using ApiGrupos.Services;

namespace ApiGrupos.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ApiSecurityService security)
    {
        if (!RequiresApiKey(context.Request.Path) || HttpMethods.IsOptions(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (!security.IsApiKeyConfigured)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("API key nao configurada no servidor.");
            return;
        }

        var headerName = security.ApiKeyHeaderName;
        var hasApiKey = context.Request.Headers.TryGetValue(headerName, out var values);
        var apiKey = hasApiKey ? values.ToString() : null;

        if (!security.ValidateApiKey(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("API key ausente ou invalida.");
            return;
        }

        await _next(context);
    }

    private static bool RequiresApiKey(PathString path)
    {
        return path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase) &&
            !AdminBasicAuthMiddleware.RequiresAdminAuthentication(path);
    }
}

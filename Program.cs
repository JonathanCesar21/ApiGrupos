using ApiGrupos.Middleware;
using ApiGrupos.Services;
using Microsoft.OpenApi.Models;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddSingleton(_ =>
    new ConnectionStringProvider(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<RequestLogWriter>();
builder.Services.Configure<ApiSecurityOptions>(
    builder.Configuration.GetSection(ApiSecurityOptions.SectionName));
builder.Services.AddSingleton<ApiSecurityService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "Informe a API key no header X-API-Key.",
        In = ParameterLocation.Header,
        Name = "X-API-Key",
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS liberado para desenvolvimento (ajuste para producao conforme necessario).
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("DevCors");
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<RequestLogWriter>();
    var stopwatch = Stopwatch.StartNew();

    await next();

    stopwatch.Stop();

    var statusCode = context.Response.StatusCode;
    var method = context.Request.Method;
    var path = context.Request.Path.Value ?? "/";
    var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "desconhecido";
    var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {statusCode} | {method} {path}{query} | {stopwatch.ElapsedMilliseconds}ms | {ip}";

    await logger.WriteAsync(line);
});
app.UseMiddleware<AdminBasicAuthMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseMiddleware<ApiKeyAuthenticationMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

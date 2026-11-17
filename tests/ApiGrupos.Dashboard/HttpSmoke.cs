using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ApiGrupos.Controllers;

internal static class HttpSmoke
{
    public static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ApiGrupos.csproj"))) directory = directory.Parent;
            return directory?.FullName ?? throw new Exception("Raiz do projeto nao encontrada.");
        }
    }

    public static async Task RunAsync(bool exportOpenApi, Action<bool, string> check)
    {
        // Instancia isolada; nao chama nem substitui a API publicada.
        var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
        var key = Guid.NewGuid().ToString("N");
        var password = Guid.NewGuid().ToString("N");
        string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
        var workdir = Path.Combine(Root, "build-check", "dashboard-http"); Directory.CreateDirectory(workdir);
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = workdir, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        start.ArgumentList.Add(typeof(DashboardController).Assembly.Location);
        start.Environment["ASPNETCORE_URLS"] = $"http://127.0.0.1:{port}";
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["ConnectionStrings__DefaultConnection"] = "Server=127.0.0.1;Database=Unused;Encrypt=False;";
        start.Environment["ApiSecurity__ApiKeyHash"] = Hash(key);
        start.Environment["ApiSecurity__ApiKeyHeaderName"] = "X-API-Key";
        start.Environment["ApiSecurity__AdminUsername"] = "dashboard-test";
        start.Environment["ApiSecurity__AdminPasswordHash"] = Hash(password);
        using var process = Process.Start(start) ?? throw new Exception("Nao foi possivel iniciar API isolada.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        try
        {
            using var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}"), Timeout = TimeSpan.FromSeconds(10) };
            var ready = false;
            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (process.HasExited) throw new Exception("API isolada encerrou: " + await error + await output);
                try { using var ping = await client.GetAsync("/api/dashboard/titulos"); ready = true; break; }
                catch (HttpRequestException) { await Task.Delay(100); }
            }
            check(ready, "API isolada nao iniciou.");
            using (var unauthorized = await client.GetAsync("/api/dashboard/titulos")) check(unauthorized.StatusCode == HttpStatusCode.Unauthorized, "X-API-Key continua obrigatoria.");
            client.DefaultRequestHeaders.Add("X-API-Key", key);
            var valid = new[]
            {
                "recebimentos?dataInicio=2026-06-10&dataFim=2026-06-15", "titulos", "metas-lojas", "metas-vendedores", "vendedores",
                "entradas-a-confirmar", "transferencias", "ativacoes?dataInicio=2026-06-10&dataFim=2026-06-15", "referencias",
                "terceiros?dataInicio=2026-06-10&dataFim=2026-06-15&status=0", "vendas-agregado?dataInicio=2026-06-10&dataFim=2026-06-15",
                "entradas-agregado?dataInicio=2026-06-10&dataFim=2026-06-15"
            };
            foreach (var path in valid)
            {
                using var response = await client.GetAsync("/api/dashboard/" + path);
                check(response.StatusCode == HttpStatusCode.ServiceUnavailable, $"Rota/contrato nao registrado: {path}; HTTP {(int)response.StatusCode}.");
            }
            foreach (var path in new[]
            {
                "recebimentos", "titulos?tipoData=invalido", "titulos?situacao=invalida", "titulos?page=1", "metas-lojas?mes=13",
                "metas-vendedores?ano=2021", "vendedores?loja=-1", "vendedores?loja=abc", "entradas-a-confirmar?dataInicio=2026-06-10",
                "transferencias?tipoTransferencia=invalido", "referencias?referencia=" + new string('x', 201),
                "vendas-agregado?dataInicio=2026-01-01&dataFim=2026-05-01", "titulos?dataInicio=2026-12-01&dataFim=2026-01-01",
                "recebimentos?dataInicio=0001-01-01&dataFim=0001-01-02", "transferencias?dataInicio=2026-01-01&dataFim=9999-12-31"
            })
            {
                using var response = await client.GetAsync("/api/dashboard/" + path);
                check(response.StatusCode == HttpStatusCode.BadRequest, $"Validacao HTTP incorreta: {path}; {(int)response.StatusCode}.");
            }
            foreach (var path in new[]
            {
                "Subgrupos", "Subgrupos?page=1&pageSize=100", "Subgrupos/por-grupo/1", "Subgrupos/por-grupo/1?page=1&pageSize=100",
                "consulta-vendas?dataInicio=2026-06-10&dataFim=2026-06-15", "consulta-entradas?dataInicio=2026-06-10&dataFim=2026-06-15",
                "recebimentos/crediarista/titulos?dataInicio=2026-06-10&dataFim=2026-06-15",
                "recebimentos/crediarista/capitalizacao?dataInicio=2026-06-10&dataFim=2026-06-15"
            })
            {
                using var response = await client.GetAsync("/api/" + path);
                check(response.StatusCode == HttpStatusCode.ServiceUnavailable, "Rota legada mudou: " + path);
            }
            using (var protectedSwagger = await client.GetAsync("/swagger/v1/swagger.json")) check(protectedSwagger.StatusCode == HttpStatusCode.Unauthorized, "Swagger continua protegido por admin.");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes("dashboard-test:" + password)));
            using var swagger = await client.GetAsync("/swagger/v1/swagger.json");
            check(swagger.IsSuccessStatusCode, "Geracao OpenAPI falhou.");
            var specification = await swagger.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(specification);
            var paths = document.RootElement.GetProperty("paths");
            check(paths.EnumerateObject().Count(p => p.Name.StartsWith("/api/dashboard/")) == 12, "OpenAPI deve conter as 12 rotas dashboard.");
            var receiptParameters = paths.GetProperty("/api/dashboard/recebimentos").GetProperty("get").GetProperty("parameters").EnumerateArray().ToArray();
            check(receiptParameters.Single(p => p.GetProperty("name").GetString() == "tipoData").GetProperty("schema").GetProperty("default").GetString() == "baixa", "OpenAPI deve documentar nomes camelCase e defaults.");
            check(document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("Subgrupo").GetProperty("properties").GetProperty("departamentoCodigo").GetProperty("type").GetString() == "integer", "departamentoCodigo deve ser numerico no OpenAPI.");
            check(document.RootElement.GetProperty("components").GetProperty("securitySchemes").GetProperty("ApiKey").GetProperty("name").GetString() == "X-API-Key", "Esquema de seguranca preservado.");
            if (exportOpenApi)
            {
                var directory = Path.Combine(Root, "docs"); Directory.CreateDirectory(directory);
                await File.WriteAllTextAsync(Path.Combine(directory, "openapi.json"), JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true }) + "\n");
                Console.WriteLine("PASS: docs/openapi.json gerado pelo Swagger da aplicacao.");
            }
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(); await Task.WhenAll(output, error);
        }
    }
}

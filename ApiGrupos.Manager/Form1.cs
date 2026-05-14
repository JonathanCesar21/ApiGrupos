using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ApiGrupos.Manager;

public partial class Form1 : Form
{
    private readonly HttpClient _httpClient = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new();
    private readonly object _tunnelLogLock = new();
    private Process? _tunnelProcess;
    private static readonly string[] EndpointPaths =
    {
        "/api/unidade",
        "/api/ncm",
        "/api/situacao-tributaria",
        "/api/situacao-tributaria/rpa",
        "/api/situacao-tributaria/simples"
    };

    public Form1()
    {
        InitializeComponent();
        ApplyDefaultPaths();

        _refreshTimer.Interval = 5000;
        _refreshTimer.Tick += async (_, _) =>
        {
            UpdateProcessStatusLabel();
            UpdateTunnelStatusLabel();
            await RefreshConfiguracaoStatusAsync(silent: true);
            RefreshRequestLog();
        };
        _refreshTimer.Start();

        UpdateProcessStatusLabel();
        UpdateTunnelStatusLabel();
        _ = RefreshConfiguracaoStatusAsync(silent: true);
        RefreshRequestLog();
    }

    private void ApplyDefaultPaths()
    {
        var repoRoot = FindRepoRoot();

        if (!string.IsNullOrWhiteSpace(repoRoot))
        {
            txtExePath.Text = Path.Combine(repoRoot, "publish-win", "ApiGrupos.exe");
            txtCloudflaredPath.Text = TryFindCloudflaredPath() ?? string.Empty;
        }
        else
        {
            var managerDir = AppContext.BaseDirectory;
            var distApiDir = Path.GetFullPath(Path.Combine(managerDir, "..", "Api"));
            var cloudflaredDir = Path.GetFullPath(Path.Combine(managerDir, "..", "Cloudflared", "cloudflared.exe"));
            txtExePath.Text = Path.Combine(distApiDir, "ApiGrupos.exe");
            txtCloudflaredPath.Text = cloudflaredDir;
        }

        txtTunnelConfigPath.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cloudflared",
            "config.yml");
        txtTunnelName.Text = "apigrupos";

        txtLogPath.Text = Path.Combine(GetLogsDirectory(), "requests.log");

        txtApiUrl.Text = "http://localhost:5000";
        RefreshEndpointsList();
    }

    private static string? TryFindCloudflaredPath()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages", "Cloudflare.cloudflared_Microsoft.Winget.Source_8wekyb3d8bbwe", "cloudflared.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Cloudflare", "cloudflared", "cloudflared.exe")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private string? FindRepoRoot()
    {
        var current = AppContext.BaseDirectory;

        for (var i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(current, "ApiGrupos.csproj")))
            {
                return current;
            }

            var parent = Directory.GetParent(current);
            if (parent == null)
            {
                break;
            }

            current = parent.FullName;
        }

        return null;
    }

    private Uri BuildUri(string relativePath)
    {
        var baseUrl = txtApiUrl.Text.Trim().TrimEnd('/');
        return new Uri($"{baseUrl}{relativePath}");
    }

    private static string GetLogsDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ApiGrupos",
            "logs");
    }

    private static string GetTunnelLogPath()
    {
        return Path.Combine(GetLogsDirectory(), "cloudflared.log");
    }

    private void RefreshEndpointsList()
    {
        var baseUrl = txtApiUrl.Text.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            txtEndpoints.Text = string.Join(Environment.NewLine, EndpointPaths);
            return;
        }

        txtEndpoints.Text = string.Join(
            Environment.NewLine,
            EndpointPaths.Select(path => $"{baseUrl}{path}"));
    }

    private void UpdateProcessStatusLabel()
    {
        var runningCount = Process.GetProcessesByName("ApiGrupos").Length;
        lblProcessStatus.Text = runningCount > 0
            ? $"Status processo: API em execucao ({runningCount})"
            : "Status processo: API parada";
    }

    private void UpdateTunnelStatusLabel()
    {
        var runningCount = Process.GetProcessesByName("cloudflared").Length;
        lblTunnelStatus.Text = runningCount > 0
            ? $"Status tunnel: ativo ({runningCount})"
            : "Status tunnel: parado";
    }

    private async Task RefreshConfiguracaoStatusAsync(bool silent = false)
    {
        try
        {
            var response = await _httpClient.GetAsync(BuildUri("/api/configuracao/status"));
            if (!response.IsSuccessStatusCode)
            {
                lblConfigStatus.Text = $"Status configuracao: erro HTTP {(int)response.StatusCode}";
                return;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var configurado = doc.RootElement.GetProperty("configurado").GetBoolean();
            lblConfigStatus.Text = configurado
                ? "Status configuracao: credenciais configuradas"
                : "Status configuracao: credenciais nao configuradas";
        }
        catch (Exception ex)
        {
            lblConfigStatus.Text = "Status configuracao: API indisponivel";
            if (!silent)
            {
                MessageBox.Show($"Falha ao consultar status: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void RefreshRequestLog()
    {
        try
        {
            var logPath = txtLogPath.Text.Trim();
            if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            {
                txtRequestLog.Text = "Arquivo de log nao encontrado.";
                return;
            }

            txtRequestLog.Text = string.Join(Environment.NewLine, ReadLastLines(logPath, 300));
            txtRequestLog.SelectionStart = txtRequestLog.Text.Length;
            txtRequestLog.ScrollToCaret();
        }
        catch (Exception ex)
        {
            txtRequestLog.Text = $"Erro ao ler log: {ex.Message}";
        }
    }

    private static IEnumerable<string> ReadLastLines(string path, int maxLines)
    {
        var queue = new Queue<string>(maxLines);

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine() ?? string.Empty;
            if (queue.Count >= maxLines)
            {
                queue.Dequeue();
            }

            queue.Enqueue(line);
        }

        return queue;
    }

    private static string? TryResolveCredentialsFilePath(string tunnelConfigPath)
    {
        foreach (var line in File.ReadLines(tunnelConfigPath))
        {
            var trimmedLine = line.Trim();
            if (!trimmedLine.StartsWith("credentials-file:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rawValue = trimmedLine["credentials-file:".Length..].Trim().Trim('"', '\'');
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return null;
            }

            var expandedValue = Environment.ExpandEnvironmentVariables(rawValue);
            if (Path.IsPathRooted(expandedValue))
            {
                return expandedValue;
            }

            var configDirectory = Path.GetDirectoryName(tunnelConfigPath) ?? AppContext.BaseDirectory;
            return Path.GetFullPath(Path.Combine(configDirectory, expandedValue));
        }

        return null;
    }

    private void StartTunnelLogSession(string cloudflaredPath, string tunnelConfigPath, string tunnelName)
    {
        Directory.CreateDirectory(GetLogsDirectory());

        var header = string.Join(
            Environment.NewLine,
            [
                string.Empty,
                $"===== {DateTime.Now:yyyy-MM-dd HH:mm:ss} =====",
                $"Executavel: {cloudflaredPath}",
                $"Config: {tunnelConfigPath}",
                $"Tunnel: {tunnelName}"
            ]) + Environment.NewLine;

        lock (_tunnelLogLock)
        {
            File.AppendAllText(GetTunnelLogPath(), header, Encoding.UTF8);
        }
    }

    private void AppendTunnelLogLine(string channel, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {channel}: {line}{Environment.NewLine}";
        lock (_tunnelLogLock)
        {
            File.AppendAllText(GetTunnelLogPath(), entry, Encoding.UTF8);
        }
    }

    private string GetTunnelLogExcerpt(int maxLines)
    {
        var tunnelLogPath = GetTunnelLogPath();
        if (!File.Exists(tunnelLogPath))
        {
            return "Log do cloudflared ainda nao foi gerado.";
        }

        return string.Join(Environment.NewLine, ReadLastLines(tunnelLogPath, maxLines));
    }

    private void btnBrowseExe_Click(object? sender, EventArgs e)
    {
        if (openFileDialogExe.ShowDialog() == DialogResult.OK)
        {
            txtExePath.Text = openFileDialogExe.FileName;
        }
    }

    private void btnBrowseCloudflared_Click(object? sender, EventArgs e)
    {
        if (openFileDialogCloudflared.ShowDialog() == DialogResult.OK)
        {
            txtCloudflaredPath.Text = openFileDialogCloudflared.FileName;
        }
    }

    private void btnBrowseTunnelConfig_Click(object? sender, EventArgs e)
    {
        if (openFileDialogTunnelConfig.ShowDialog() == DialogResult.OK)
        {
            txtTunnelConfigPath.Text = openFileDialogTunnelConfig.FileName;
        }
    }

    private void btnStartApi_Click(object? sender, EventArgs e)
    {
        try
        {
            var exePath = txtExePath.Text.Trim();
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            {
                MessageBox.Show("Informe um caminho valido para o ApiGrupos.exe.", "Atencao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var workingDir = Path.GetDirectoryName(exePath)!;

            var processInfo = new ProcessStartInfo(exePath)
            {
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            processInfo.EnvironmentVariables["ASPNETCORE_URLS"] = "http://0.0.0.0:5000";

            Process.Start(processInfo);

            Thread.Sleep(1200);
            UpdateProcessStatusLabel();
            MessageBox.Show("API iniciada.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao iniciar API: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnStopApi_Click(object? sender, EventArgs e)
    {
        try
        {
            var processes = Process.GetProcessesByName("ApiGrupos");
            foreach (var process in processes)
            {
                process.Kill(true);
            }

            UpdateProcessStatusLabel();
            MessageBox.Show(processes.Length > 0 ? "API parada." : "Nao havia processo da API em execucao.", "Informacao", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao parar API: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnStartTunnel_Click(object? sender, EventArgs e)
    {
        try
        {
            var cloudflaredPath = txtCloudflaredPath.Text.Trim();
            var tunnelConfigPath = txtTunnelConfigPath.Text.Trim();
            var tunnelName = txtTunnelName.Text.Trim();

            if (string.IsNullOrWhiteSpace(cloudflaredPath) || !File.Exists(cloudflaredPath))
            {
                MessageBox.Show("Informe o caminho valido para cloudflared.exe.", "Atencao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(tunnelConfigPath) || !File.Exists(tunnelConfigPath))
            {
                MessageBox.Show("Informe um caminho valido para o config.yml do tunnel.", "Atencao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(tunnelName))
            {
                MessageBox.Show("Informe o nome do tunnel.", "Atencao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var credentialsFilePath = TryResolveCredentialsFilePath(tunnelConfigPath);
            if (!string.IsNullOrWhiteSpace(credentialsFilePath) && !File.Exists(credentialsFilePath))
            {
                MessageBox.Show(
                    $"O config.yml referencia um arquivo de credenciais inexistente:{Environment.NewLine}{credentialsFilePath}{Environment.NewLine}{Environment.NewLine}Ajuste o campo credentials-file no config.yml ou copie esse .json para a nova maquina.",
                    "Atencao",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var processInfo = new ProcessStartInfo(cloudflaredPath)
            {
                WorkingDirectory = Path.GetDirectoryName(cloudflaredPath)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                Arguments = $"--config \"{tunnelConfigPath}\" tunnel run {tunnelName}"
            };

            StartTunnelLogSession(cloudflaredPath, tunnelConfigPath, tunnelName);

            var process = new Process
            {
                StartInfo = processInfo,
                EnableRaisingEvents = true
            };
            process.OutputDataReceived += (_, args) => AppendTunnelLogLine("OUT", args.Data);
            process.ErrorDataReceived += (_, args) => AppendTunnelLogLine("ERR", args.Data);

            if (!process.Start())
            {
                MessageBox.Show("Nao foi possivel iniciar o processo do tunnel.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            _tunnelProcess = process;

            Thread.Sleep(2000);
            UpdateTunnelStatusLabel();

            if (process.HasExited)
            {
                var excerpt = GetTunnelLogExcerpt(12);
                MessageBox.Show(
                    $"O cloudflared encerrou logo apos iniciar.{Environment.NewLine}{Environment.NewLine}Verifique o config.yml, o nome do tunnel e o arquivo de credenciais.{Environment.NewLine}Log: {GetTunnelLogPath()}{Environment.NewLine}{Environment.NewLine}Ultimas linhas:{Environment.NewLine}{excerpt}",
                    "Erro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(
                $"Tunnel iniciado.{Environment.NewLine}Log: {GetTunnelLogPath()}",
                "Sucesso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao iniciar tunnel: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnStopTunnel_Click(object? sender, EventArgs e)
    {
        try
        {
            var processes = Process.GetProcessesByName("cloudflared");
            foreach (var process in processes)
            {
                process.Kill(true);
            }

            _tunnelProcess = null;
            UpdateTunnelStatusLabel();
            MessageBox.Show(processes.Length > 0 ? "Tunnel parado." : "Nao havia tunnel em execucao.", "Informacao", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao parar tunnel: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void btnSalvarCredenciais_Click(object? sender, EventArgs e)
    {
        try
        {
            var usuario = txtUsuario.Text.Trim();
            var senha = txtSenha.Text;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha))
            {
                MessageBox.Show("Usuario e senha sao obrigatorios.", "Atencao", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var payload = JsonSerializer.Serialize(new { usuario, senha });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(BuildUri("/api/configuracao/credenciais"), content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show(string.IsNullOrWhiteSpace(body) ? $"Erro HTTP {(int)response.StatusCode}" : body, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            await RefreshConfiguracaoStatusAsync(silent: true);
            MessageBox.Show("Credenciais salvas com sucesso.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao salvar credenciais: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void btnConsultarStatus_Click(object? sender, EventArgs e)
    {
        await RefreshConfiguracaoStatusAsync();
    }

    private void btnAtualizarLogs_Click(object? sender, EventArgs e)
    {
        RefreshRequestLog();
    }

    private void btnOpenSwagger_Click(object? sender, EventArgs e)
    {
        try
        {
            var swaggerUrl = BuildUri("/swagger").ToString();
            Process.Start(new ProcessStartInfo
            {
                FileName = swaggerUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao abrir swagger: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void txtApiUrl_TextChanged(object? sender, EventArgs e)
    {
        RefreshEndpointsList();
    }

    private void btnCopiarEndpoints_Click(object? sender, EventArgs e)
    {
        try
        {
            var content = txtEndpoints.Text.Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("Nao ha endpoints para copiar.", "Informacao", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Clipboard.SetText(content);
            MessageBox.Show("Endpoints copiados para a area de transferencia.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao copiar endpoints: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

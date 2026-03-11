namespace ApiGrupos.Services;

public class RequestLogWriter
{
    private readonly string _logFilePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public RequestLogWriter(IHostEnvironment environment)
    {
        var logsDirectory = Path.Combine(environment.ContentRootPath, "logs");
        Directory.CreateDirectory(logsDirectory);
        _logFilePath = Path.Combine(logsDirectory, "requests.log");
    }

    public string LogFilePath => _logFilePath;

    public async Task WriteAsync(string line, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            await File.AppendAllTextAsync(_logFilePath, line + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

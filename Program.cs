using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

var baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(baseConnectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' nao foi configurada.");
}

var csBuilder = new SqlConnectionStringBuilder(baseConnectionString);

var hasUserInConfig = !string.IsNullOrWhiteSpace(csBuilder.UserID) && !csBuilder.UserID.Contains("COLOCAR_USUARIO_AQUI", StringComparison.OrdinalIgnoreCase);
var hasPasswordInConfig = !string.IsNullOrWhiteSpace(csBuilder.Password) && !csBuilder.Password.Contains("COLOCAR_SENHA_AQUI", StringComparison.OrdinalIgnoreCase);

if (!hasUserInConfig || !hasPasswordInConfig)
{
    // Para ambiente local interativo, pergunta credenciais sem persistir em arquivo.
    if (Environment.UserInteractive)
    {
        Console.Write("Informe o usuario do SQL Server: ");
        var sqlUser = Console.ReadLine();

        Console.Write("Informe a senha do SQL Server: ");
        var sqlPassword = ReadPassword();
        Console.WriteLine();

        if (string.IsNullOrWhiteSpace(sqlUser) || string.IsNullOrWhiteSpace(sqlPassword))
        {
            throw new InvalidOperationException("Usuario e senha sao obrigatorios para iniciar a API.");
        }

        csBuilder.UserID = sqlUser;
        csBuilder.Password = sqlPassword;
    }
    else
    {
        throw new InvalidOperationException(
            "ConnectionStrings__DefaultConnection deve conter User Id e Password validos em ambiente nao interativo.");
    }
}

// Sobrescreve apenas em memoria durante a execucao (nao grava em arquivo).
builder.Configuration["ConnectionStrings:DefaultConnection"] = csBuilder.ConnectionString;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("DevCors");
app.UseAuthorization();
app.MapControllers();

app.Run();

static string ReadPassword()
{
    var passwordChars = new List<char>();

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
            break;
        }

        if (key.Key == ConsoleKey.Backspace && passwordChars.Count > 0)
        {
            passwordChars.RemoveAt(passwordChars.Count - 1);
            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            passwordChars.Add(key.KeyChar);
        }
    }

    return new string(passwordChars.ToArray());
}

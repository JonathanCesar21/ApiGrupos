var builder = WebApplication.CreateBuilder(args);

var baseConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(baseConnectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' não foi configurada.");
}

Console.Write("Informe o usuário do SQL Server: ");
var sqlUser = Console.ReadLine();

Console.Write("Informe a senha do SQL Server: ");
var sqlPassword = ReadPassword();
Console.WriteLine();

if (string.IsNullOrWhiteSpace(sqlUser) || string.IsNullOrWhiteSpace(sqlPassword))
{
    throw new InvalidOperationException("Usuário e senha são obrigatórios para iniciar a API.");
}

var csBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(baseConnectionString)
{
    UserID = sqlUser,
    Password = sqlPassword
};

// Sobrescreve apenas em memória durante a execução (não grava em arquivo).
builder.Configuration["ConnectionStrings:DefaultConnection"] = csBuilder.ConnectionString;

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS liberado para desenvolvimento (ajuste para produção conforme necessário).
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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

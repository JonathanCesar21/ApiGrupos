using ApiGrupos.Models;
using ApiGrupos.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiGrupos.Controllers;

[ApiController]
public class ConfiguracaoController : ControllerBase
{
    private readonly ConnectionStringProvider _connectionStringProvider;

    public ConfiguracaoController(ConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    [HttpGet("configuracao")]
    public ContentResult PaginaConfiguracao()
    {
        const string html = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Configuracao do Banco</title>
    <style>
        body { font-family: Segoe UI, Arial, sans-serif; background: #f4f6f8; margin: 0; }
        .card { max-width: 480px; margin: 48px auto; background: #fff; padding: 24px; border-radius: 10px; box-shadow: 0 8px 24px rgba(0,0,0,.08); }
        h1 { margin-top: 0; font-size: 20px; }
        label { display: block; margin: 12px 0 6px; font-weight: 600; }
        input { width: 100%; box-sizing: border-box; padding: 10px; border: 1px solid #c8ced6; border-radius: 6px; }
        button { margin-top: 16px; width: 100%; padding: 10px; border: 0; border-radius: 6px; background: #0b5ed7; color: #fff; font-weight: 600; cursor: pointer; }
        .msg { margin-top: 12px; font-size: 14px; }
    </style>
</head>
<body>
    <div class="card">
        <h1>Configurar acesso ao banco</h1>
        <p>Informe usuario e senha para ativar a API nesta maquina.</p>

        <label for="usuario">Usuario SQL</label>
        <input id="usuario" type="text" autocomplete="off" />

        <label for="senha">Senha SQL</label>
        <input id="senha" type="password" autocomplete="off" />

        <button id="salvar">Salvar credenciais</button>
        <div id="msg" class="msg"></div>
    </div>

    <script>
        const button = document.getElementById('salvar');
        const msg = document.getElementById('msg');

        button.addEventListener('click', async () => {
            msg.textContent = 'Salvando...';

            const usuario = document.getElementById('usuario').value;
            const senha = document.getElementById('senha').value;

            const response = await fetch('/api/configuracao/credenciais', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ usuario, senha })
            });

            if (response.ok) {
                msg.textContent = 'Credenciais salvas em memoria. API pronta para uso.';
                return;
            }

            const text = await response.text();
            msg.textContent = text || 'Falha ao salvar credenciais.';
        });
    </script>
</body>
</html>
""";

        return Content(html, "text/html");
    }

    [HttpPost("api/configuracao/credenciais")]
    public ActionResult SalvarCredenciais([FromBody] CredenciaisRequest request)
    {
        try
        {
            _connectionStringProvider.SetCredentials(request.Usuario, request.Senha);
            return Ok(new { mensagem = "Credenciais configuradas com sucesso." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, $"Erro ao configurar credenciais: {ex.Message}");
        }
    }

    [HttpGet("api/configuracao/status")]
    public ActionResult Status()
    {
        return Ok(new { configurado = _connectionStringProvider.IsConfigured });
    }
}

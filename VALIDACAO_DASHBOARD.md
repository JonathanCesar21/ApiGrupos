# Validação das ampliações Book Lojas

Data: 17/11/2026. O operador informou que não há conexão de homologação e autorizou concluir a implementação com integração SQL pendente. **Não foi possível conferir o esquema real nem executar SELECTs no ERP.** Nenhuma credencial foi solicitada em texto, nenhuma tabela do ERP foi modificada e a API publicada não foi substituída.

## Comandos locais

Na raiz do repositório, com SDK .NET 8:

```powershell
dotnet build ApiGrupos.csproj -c Release
dotnet run --project tests/ApiGrupos.Dashboard/ApiGrupos.Dashboard.csproj -c Release -- --export-openapi
dotnet run --project tests/ApiGrupos.ClientesCrmHistorico/ApiGrupos.ClientesCrmHistorico.csproj -c Release
dotnet run --project tests/ApiGrupos.SqlCompatibility/ApiGrupos.SqlCompatibility.csproj -c Release
```

O teste dashboard inicia uma instância HTTP isolada em loopback/porta livre, com chave e senha administrativas sintéticas, sem credenciais SQL. Encerra somente o processo criado por ele. Não inicia túnel, não publica a API e não chama o serviço de produção. O OpenAPI é gerado pelo Swagger da aplicação em `docs/openapi.json`, sem segredos.

Resultados executados nesta entrega:

| Verificação | Resultado |
|---|---|
| Build Release da API | Passou, 0 avisos e 0 erros. |
| Suíte Dashboard | **167 verificações passaram**, incluindo HTTP local e geração do OpenAPI. |
| Suíte existente de CRM | **27 verificações passaram**; integração SQL existente também ficou pendente. |
| Suíte existente de compatibilidade SQL | Passou nos parsers 2005/2008 e detectou a regressão de TRY_CONVERT. |
| Esquema/execução real SQL e fixtures no servidor | **Pendente; não executado.** |

Cobertura local:

- Contratos HTTP das 12 novas rotas, erros de binding/validação, 401 sem API key, 503 sem banco; proteção administrativa do Swagger; presença e campos dos contratos no OpenAPI.
- Presença das quatro variantes de Subgrupos e de rotas legadas representativas; o sucesso das consultas legadas no banco continua pendente.
- Intervalos inclusivos de 120/121 dias, datas inválidas/invertidas/máximas, parâmetros tipados e listas fixas contra SQL arbitrário.
- Preservação de null/false, valores e quantidades fracionárias/negativas, barras/pedidos textuais, espaços das formas de pagamento, datas sem deslocamento UTC, cancelamento já solicitado.
- Cálculos de linha de meta, valorTotal, quant de ativação, status/tipo de transferência e concatenação que ignora somente null.
- Sintaxe e funções usadas nas consultas compatíveis com os parsers SQL Server 2005/2008; sintaxe dos scripts de esquema, fixtures e quatro caminhos de Subgrupos. **Isso não confirma nomes/tipos no banco.**
- Regressões existentes de CRM e de sintaxe/funções de vendas com transferências automáticas.

## Configurar a integração SQL depois

Use uma conexão autorizada, preferencialmente de homologação, com permissão de leitura do esquema e das fontes. O teste cria apenas tabelas `#temporarias` para dados sintéticos; não cria tabelas/views nem altera registros do ERP. A conexão precisa permitir essas temporárias em tempdb.

Defina `APIGRUPOS_TEST_SQL_CONNECTION` **somente no ambiente local do processo**. Exemplo com prompt protegido do PowerShell, sem colocar senha no histórico/comando/arquivo:

```powershell
$credencialDashboard = Get-Credential -Message 'Acesso SQL de homologacao da ApiGrupos'
$conexaoDashboard = New-Object System.Data.SqlClient.SqlConnectionStringBuilder
$conexaoDashboard.DataSource = 'tcp:SERVIDOR_AUTORIZADO,1433'
$conexaoDashboard.InitialCatalog = 'BASE_AUTORIZADA'
$conexaoDashboard.UserID = $credencialDashboard.UserName
$conexaoDashboard.Password = $credencialDashboard.GetNetworkCredential().Password
# Ajustar TLS/certificado conforme a configuracao autorizada do servidor.
$conexaoDashboard.Encrypt = $true
$conexaoDashboard.TrustServerCertificate = $false
$env:APIGRUPOS_TEST_SQL_CONNECTION = $conexaoDashboard.ConnectionString
try {
    dotnet run --project tests/ApiGrupos.Dashboard/ApiGrupos.Dashboard.csproj -c Release -- --sql
} finally {
    Remove-Item Env:APIGRUPOS_TEST_SQL_CONNECTION -ErrorAction SilentlyContinue
    $conexaoDashboard.Clear()
    $credencialDashboard = $null
}
```

A variável de testes não reconfigura a API publicada. O provedor da aplicação continua sendo o existente: ConnectionStrings:DefaultConnection e configuração administrativa de credenciais em memória. Para usar autenticação integrada, configure a string pelo mecanismo seguro disponível na homologação, sem o exemplo de usuário/senha acima.

`--sql` falha explicitamente se a variável estiver ausente. Sem essa opção, o teste imprime `PENDENTE`, não declara uma integração bem sucedida. Não copie a string ou a chave para chats, logs ou arquivos versionados.

O modo SQL executa:

1. `scripts/conferir-dashboard-esquema.sql`: versão, tipos/nulos, índices e contagens de multiplicidade; retorna somente metadados e totais, sem dados individuais de clientes. A janela da contagem de controle/transferência está limitada a 10–15/06/2026 e pode ser ajustada pelo operador.
2. Cada projeção dashboard com `CommandBehavior.SchemaOnly` contra as fontes reais: valida resolução de nomes e metadados, sem carregar fatos completos.
3. As consultas reais contra fixtures sintéticas em #temporarias na mesma conexão, validando os resultados e os mapeamentos, inclusive as quatro consultas de Subgrupos e suas contagens. As temporárias deixam de existir ao fechar a conexão.

Fixtures incluídas: formas CIELO/STONE PIX e dinheiro com espaço, pedidos nulos/REC AG., valores de título/pagamento distintos, loja de recebimento diferente, título antigo/pago, Pago=false e Baixa=NULL e o inverso, metas aditivas, homônimos, vendedor inativo transferido, entrada antiga/negativa, ambos os ramos de transferência, UsuarioAuto/DataAuto divergentes, multiplicidade do JOIN, primeira compra antes de 2022 e dois pedidos na primeira data, referência com dois códigos, devolução com receita residual e quantidade líquida zero, deduplicação de entrada cruzando janelas e partições diferentes por nota/barras, terceiros com Recebimento preenchido/órfãos no JOIN.

## Pendências antes de implantar

| Verificação | Estado / ação |
|---|---|
| Nomes, tipos e nulos reais | Pendente. Rodar `--sql`; ajustar DTOs/conversões se o ERP diferir dos anexos. |
| Departamento numérico | Pendente confirmar **Subgrupos.grupo**, distinto de Subgrupos.CodGrupo e Grupos.codgrupo. O SELECT novo não tem fallback para um código inventado. |
| Chaves e cardinalidade | Pendente conferir unicidade de códigos/dimensões, índices filtrados em servidores >=2008, produtos por referência, barras, vendedores e transferências por controle. Não prometer paginação estável de fatos. |
| Referências / entradas empatadas | Pendente conferir códigos repetidos e empates em Data; desempate do SQL original não é garantido em empates. |
| Ciclo de EntradaControle | Pendente confirmar como o ERP remove/encerra linhas. Não foi inventada coluna Status. |
| Plano e duração das consultas | Pendente em volume real, sobretudo primeira compra histórica, fotografias de títulos e deduplicação global de entradas. Timeout não comprova desempenho. |
| Sucesso HTTP contra SQL | Pendente executar os endpoints numa instância de homologação configurada e comparar totais com os SELECTs originais em janela curta, incluindo as quatro variantes de Subgrupos e soma das páginas. |
| Regressão com dados reais | Pendente comparar payloads/totais das rotas usadas pelos sistemas existentes. Suas regras não foram alteradas; Subgrupos recebeu o campo novo e correção de aliases/contagem. |
| Reconciliação analítica | Pendente comparar o dashboard com o grafo DAX, inclusive prefixos LEFT de recebimentos, contexto de pedido/loja, DISTINCTCOUNT do período, dupla soma de trânsito e rankings/RLS. |
| Implantação | Não executada. Compilar/testar não substitui a autorização e a homologação do operador. |

## Arquivos envolvidos

- `Controllers/DashboardController.cs`: 12 rotas novas com timeout, cancelamento, validação e erros explícitos.
- `Services/DashboardSql.cs`, `DashboardReader.cs`: SELECTs parametrizados e mapeamento de tipos/nulos.
- `Models/DashboardQuery.cs`, `DashboardDtos.cs`: filtros e contratos.
- `Controllers/SubgruposController.cs`, `Models/Subgrupo.cs`: correção de aliases/total, desempate por código, intervalo de página de 64 bits e departamentoCodigo nos quatro caminhos.
- `Services/DashboardOpenApi.cs`, uma inscrição em `Program.cs`: descrições, filtros, limites e schemas do Swagger; segurança e pipeline existentes preservados.
- `tests/ApiGrupos.Dashboard/*`, `scripts/conferir-dashboard-esquema.sql`: verificações locais, runner SQL opcional, fixtures e auditoria somente leitura.
- `DOCUMENTACAO_API.md`, `INTEGRACAO_DASHBOARD_LOVABLE.md`, `VALIDACAO_DASHBOARD.md`, `docs/openapi.json`, `ApiGrupos.http`: documentação e exemplos.

Os demais controladores e as regras de negócio legadas não foram editados. Em especial, a lista de capitalização legada continua com 17 formas; o dashboard calcula suas 19 formas a partir da nova fonte completa.

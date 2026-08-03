# Documentacao da API ApiGrupos

## Visao geral

- URL local padrao: `http://localhost:5122`
- Swagger: `/swagger` protegido por usuario/senha admin.
- Formato das respostas: JSON, exceto a pagina `/configuracao`.
- Os endpoints comuns da API exigem o header `X-API-Key`.
- As credenciais do SQL Server sao configuradas e mantidas apenas em memoria.

## Seguranca

### API key

Todos os endpoints em `/api/*`, exceto `/api/configuracao/*`, exigem uma API key no header:

```http
X-API-Key: SUA_API_KEY
```

A aplicacao cliente deve guardar a chave em variavel de ambiente ou arquivo local nao versionado. O servidor deve guardar apenas o SHA-256 da chave em `ApiSecurity:ApiKeyHash`.

Configuracao por variaveis de ambiente:

```powershell
$env:ApiSecurity__ApiKeyHash = "SHA256_DA_API_KEY"
```

### Acesso admin

As rotas `/swagger`, `/swagger/*`, `/configuracao` e `/api/configuracao/*` exigem Basic Auth de admin.

Configuracao por variaveis de ambiente:

```powershell
$env:ApiSecurity__AdminUsername = "admin"
$env:ApiSecurity__AdminPasswordHash = "SHA256_DA_SENHA_ADMIN"
```

Para calcular o SHA-256 de um segredo no PowerShell:

```powershell
$segredo = "troque-este-valor"
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$bytes = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($segredo))
(-join ($bytes | ForEach-Object { $_.ToString("x2") }))
```

Nao envie a API key por query string e nao salve chaves reais em arquivos versionados.

## Regras comuns

### Paginacao opcional

Os endpoints de cadastros aceitam:

- `page`: numero da pagina, iniciando em `1`.
- `pageSize`: quantidade de itens por pagina, entre `1` e `150000`.

Se nenhum parametro de paginacao for enviado, o endpoint retorna diretamente um array com todos os registros.
Se apenas um dos parametros for enviado, `page` assume `1` e `pageSize` assume `100` quando ausentes.

Exemplo:

```http
GET /api/categoria?page=1&pageSize=100
```

Resposta paginada:

| Campo | Tipo | Descricao |
|---|---|---|
| `page` | inteiro | Pagina atual. |
| `pageSize` | inteiro | Quantidade maxima de itens da pagina. |
| `total` | inteiro | Total de registros encontrados. |
| `totalPages` | inteiro | Total de paginas. |
| `items` | array | Registros da pagina atual. |

### Filtro por periodo

Os endpoints de vendas e entradas exigem:

- `dataInicio`: primeiro dia incluido no resultado, no formato `YYYY-MM-DD`.
- `dataFim`: ultimo dia incluido no resultado, no formato `YYYY-MM-DD`.

Nao existe limite de dias para o intervalo. As consultas possuem timeout de 60 segundos e podem ser canceladas ao abortar a requisicao HTTP.
Os endpoints agregados aceitam consultas a partir de `2024-01-01`; datas iniciais anteriores sao recusadas.

### Status HTTP comuns

| Status | Significado |
|---|---|
| `200` | Consulta executada com sucesso. |
| `400` | Parametros invalidos ou ausentes. |
| `401` | Credenciais admin ausentes/invalidas ou API key ausente/invalida. |
| `500` | Erro ao executar ou processar a consulta. |
| `503` | Credenciais do banco ou seguranca da API ainda nao configuradas. |
| `504` | Consulta de vendas, entradas, estoque das lojas ou controle de volumes excedeu 60 segundos. |

## Configuracao

### `GET /configuracao`

Exibe uma pagina HTML para informar usuario e senha do SQL Server.

### `POST /api/configuracao/credenciais`

Configura as credenciais do SQL Server em memoria.

Corpo:

| Campo | Tipo | Descricao |
|---|---|---|
| `usuario` | texto | Usuario do SQL Server. |
| `senha` | texto | Senha do SQL Server. |

Resposta:

| Campo | Tipo | Descricao |
|---|---|---|
| `mensagem` | texto | Confirmacao da configuracao. |

### `GET /api/configuracao/status`

Informa se as credenciais do banco estao configuradas.

| Campo | Tipo | Descricao |
|---|---|---|
| `configurado` | booleano | `true` quando a API possui credenciais em memoria. |

## Clientes

### `GET /api/clientes`

Lista clientes de forma paginada para uso no CRM. A resposta sempre usa paginacao, mesmo quando `page` e `pageSize` nao sao informados.

Parametros:

| Parametro | Obrigatorio | Descricao |
|---|---|---|
| `busca` | nao | Busca por nome, codigo, telefone, telefone de referencia, bairro ou cidade. |
| `loja` | nao | Filtra pela loja cadastrada no cliente (`Clientes.Loja`). |
| `page` | nao | Pagina atual. Padrao `1`. |
| `pageSize` | nao | Quantidade de clientes por pagina. Padrao `100`. |

Exemplo:

```http
GET /api/clientes?busca=maria&loja=10&page=1&pageSize=50
```

Resposta:

| Campo | Descricao |
|---|---|
| `page` | Pagina atual. |
| `pageSize` | Quantidade maxima de itens da pagina. |
| `total` | Total de clientes encontrados. |
| `totalPages` | Total de paginas. |
| `items` | Lista de clientes. |

Campos de cliente:

| Campo | Descricao |
|---|---|
| `codigo` | Codigo do cliente. |
| `nome` | Nome do cliente. |
| `bairro` | Bairro do cliente. |
| `nomeCidade` | Cidade do cliente. |
| `dtNascimento` | Data de nascimento. |
| `sexo` | Homem, Mulher ou Nao informado. |
| `codGrupo` | Grupo do cliente. |
| `limite` | Limite cadastrado. |
| `renda` | Renda cadastrada. |
| `idade` | Idade cadastrada. |
| `loja` | Loja cadastrada no cliente. |
| `fone` | Telefone principal. |
| `foneReferencia1` | Telefone de referencia 1. |
| `foneReferencia2` | Telefone de referencia 2. |

### `GET /api/clientes/{codigo}`

Retorna a ficha cadastral de um cliente pelo codigo.

Exemplo:

```http
GET /api/clientes/12345
```

## Cadastros

Todos os endpoints desta secao aceitam paginacao opcional.

### `GET /api/categoria`

Lista as categorias de produtos, ordenadas pelo nome.

| Campo | Tipo | Descricao |
|---|---|---|
| `CodCategoria` | inteiro | Codigo da categoria. |
| `NomeCategoria` | texto | Nome da categoria. |

### `GET /api/classificacao-custo`

Lista as classificacoes de custo, ordenadas pela descricao.

| Campo | Tipo | Descricao |
|---|---|---|
| `codigo` | inteiro | Codigo da classificacao de custo. |
| `descricao` | texto | Descricao da classificacao de custo. |

### `GET /api/colecao`

Lista as colecoes, ordenadas pelo nome.

| Campo | Tipo | Descricao |
|---|---|---|
| `colecao` | texto | Nome da colecao. |

### `GET /api/cor`

Lista as cores, ordenadas pela descricao.

| Campo | Tipo | Descricao |
|---|---|---|
| `CodCor` | inteiro | Codigo da cor. |
| `NomeCor` | texto | Nome ou descricao da cor. |

### `GET /api/fornecedor`

Lista os fornecedores, ordenados pelo nome.

| Campo | Tipo | Descricao |
|---|---|---|
| `cod_for` | inteiro | Codigo do fornecedor. |
| `nome_for` | texto | Nome do fornecedor. |

### `GET /api/Grupos`

Lista os grupos de produtos, ordenados pelo nome.

| Campo | Tipo | Descricao |
|---|---|---|
| `id` | inteiro | Codigo do grupo. |
| `nome` | texto | Nome do grupo. |

### `GET /api/Subgrupos`

Lista todos os subgrupos, ordenados pelo nome.

| Campo | Tipo | Descricao |
|---|---|---|
| `id` | inteiro | Codigo do subgrupo. |
| `nome` | texto | Nome do subgrupo. |
| `grupoCodigo` | inteiro ou nulo | Codigo do grupo ao qual pertence. |
| `codNcm` | texto ou nulo | Codigo NCM associado ao subgrupo. |

### `GET /api/Subgrupos/por-grupo/{grupoCodigo}`

Lista somente os subgrupos pertencentes ao grupo informado na URL. Retorna as mesmas colunas de `/api/Subgrupos`.

| Parametro | Local | Tipo | Descricao |
|---|---|---|---|
| `grupoCodigo` | URL | inteiro | Codigo do grupo usado no filtro. |

### `GET /api/ncm`

Lista os NCMs, ordenados pelo nome. O nome retornado combina codigo e descricao.

| Campo | Tipo | Descricao |
|---|---|---|
| `CodNcm` | inteiro | Codigo NCM. |
| `NomeNcm` | texto | Codigo e nome no formato `codigo - nome`. |

### `GET /api/numeracao`

Lista numeracoes ou tamanhos, ordenados pela descricao.

| Campo | Tipo | Descricao |
|---|---|---|
| `CodNumero` | inteiro | Codigo da numeracao. |
| `Numero` | texto | Descricao da numeracao ou tamanho. |

### `GET /api/secao`

Lista as secoes de produtos, ordenadas pela descricao.

| Campo | Tipo | Descricao |
|---|---|---|
| `codigo` | inteiro | Codigo da secao. |
| `descricao` | texto | Descricao da secao. |

### `GET /api/unidade`

Lista as unidades, ordenadas pelo nome.

| Campo | Tipo | Descricao |
|---|---|---|
| `CodUnidade` | inteiro | Codigo sequencial da unidade. |
| `NomeUnidade` | texto | Nome da unidade. |

## Situacoes tributarias

Estes endpoints aceitam paginacao opcional.

### `GET /api/situacao-tributaria`

Lista todas as situacoes tributarias.

| Campo | Tipo | Descricao |
|---|---|---|
| `CodSituacaoTributaria` | inteiro | Codigo da situacao tributaria. |
| `Descricao` | texto | Descricao da situacao tributaria. |

### `GET /api/situacao-tributaria/rpa`

Lista todas as situacoes tributarias no formato usado por RPA.

| Campo | Tipo | Descricao |
|---|---|---|
| `CodSituacaoTributariaRPA` | inteiro | Codigo da situacao tributaria. |
| `SituacaoTributariaRPA` | texto | Codigo e descricao no formato `codigo - descricao`. |

### `GET /api/situacao-tributaria/simples`

Lista somente as situacoes tributarias marcadas para Simples Nacional (`SN = '1'`).

| Campo | Tipo | Descricao |
|---|---|---|
| `CodSituacaoTributariaSimples` | inteiro | Codigo da situacao tributaria. |
| `SituacaoTributariaSimples` | texto | Codigo e descricao no formato `codigo - descricao`. |

## Produtos

### `GET /api/produto-barras`

Lista variacoes de produtos e codigos de barras vinculados aos produtos cadastrados desde `2025-01-01`. Aceita paginacao opcional e ordena por barras.

Parametros de query:

| Parametro | Tipo | Descricao |
|---|---|---|
| `page` | inteiro opcional | Pagina desejada. Quando informado com `pageSize`, retorna resposta paginada. |
| `pageSize` | inteiro opcional | Quantidade de itens por pagina. Valor maximo: `150000`. |
| `referencias` | texto opcional | Lista de referencias separadas por virgula. Exemplo: `/api/produto-barras?referencias=8523.108BR,26-8523.127BR`. Quando preenchido, ignora paginacao e remove o filtro de cadastro desde `2025-01-01`, retornando todas as variacoes dessas referencias, inclusive antigas. O filtro faz match exato da referencia usando apenas `TRIM` e comparacao case-insensitive; prefixos como `26-` nao sao removidos nem normalizados. |

Sem `referencias`, mantem o comportamento padrao com filtro de cadastro desde `2025-01-01`; se `page` ou `pageSize` forem informados, retorna o envelope paginado. Com `referencias`, retorna uma lista simples de itens.

| Campo | Tipo | Descricao |
|---|---|---|
| `CodProd` | inteiro ou nulo | Codigo do produto. |
| `referencia` | texto | Referencia do produto. |
| `barras` | texto | Codigo de barras. |
| `SubGrupo` | texto | Subgrupo do produto. |
| `DescProd` | texto | Descricao do produto. |
| `Grupo` | texto | Grupo do produto. |
| `CodSecao` | inteiro ou nulo | Codigo da secao. |
| `CodClassificacao` | inteiro ou nulo | Codigo da classificacao. |
| `CodCategoria` | inteiro ou nulo | Codigo da categoria. |
| `Numero` | texto | Numeracao ou tamanho. |
| `Cor` | texto | Cor do produto. |
| `Colecao` | texto | Colecao do produto. |
| `NomeFornecedor` | texto | Nome do fornecedor. |
| `CodFornecedor` | inteiro ou nulo | Codigo do fornecedor. |
| `ValorCusto` | decimal | Valor de custo. |
| `Valor` | decimal | Valor de venda. |

## Estoque

### `GET /api/estoque-matriz`

Retorna somente o estoque positivo atual da loja `5`, tratada como matriz.

| Campo | Tipo | Descricao |
|---|---|---|
| `codProd` | inteiro | Codigo do produto. |
| `codCor` | inteiro ou nulo | Codigo da cor. |
| `cor` | texto ou nulo | Descricao da cor. |
| `descProd` | texto ou nulo | Descricao do produto. |
| `barras` | texto ou nulo | Codigo de barras. |
| `grupo` | texto ou nulo | Grupo do produto. |
| `subGrupo` | texto ou nulo | Subgrupo do produto. |
| `codNumero` | texto ou nulo | Codigo da numeracao. |
| `numero` | texto ou nulo | Descricao da numeracao. |
| `referencia` | texto ou nulo | Referencia do produto. |
| `fornecedor` | texto ou nulo | Fornecedor do produto. |
| `valorCusto` | decimal | Valor de custo. |
| `valor` | decimal | Valor de venda. |
| `colecao` | texto ou nulo | Colecao do produto. |
| `quant` | inteiro | Quantidade atual positiva na loja 5. |
| `loja` | texto ou nulo | Codigo da loja, sempre `5`. |
| `categoria` | texto ou nulo | Codigo ou valor da categoria do produto. |
| `ultimaDataEntrada` | data/hora ou nulo | Data da entrada mais recente do item na loja. |

### `GET /api/estoque-lojas`

Retorna o estoque atual diferente de zero das lojas `1`, `2`, `3`, `4`, `5`, `7`, `8`, `9`, `10`, `11`, `12`, `13`, `15`, `16`, `17`, `18`, `19` e `20`. Inclui quantidades positivas e negativas. Possui timeout de 60 segundos e suporta cancelamento.

| Campo | Tipo | Descricao |
|---|---|---|
| `codProd` | inteiro | Codigo do produto. |
| `barras` | texto ou nulo | Codigo de barras. |
| `grupo` | texto ou nulo | Grupo do produto. |
| `subGrupo` | texto ou nulo | Subgrupo do produto. |
| `codNumero` | texto ou nulo | Codigo da numeracao. |
| `numero` | texto ou nulo | Descricao da numeracao. |
| `referencia` | texto ou nulo | Referencia do produto. |
| `fornecedor` | texto ou nulo | Fornecedor do produto. |
| `valorCusto` | decimal | Valor de custo. |
| `valor` | decimal | Valor de venda. |
| `colecao` | texto ou nulo | Colecao do produto. |
| `quant` | inteiro | Quantidade atual na loja. |
| `loja` | texto ou nulo | Codigo da loja. |
| `categoria` | texto ou nulo | Codigo ou valor da categoria do produto. |
| `ultimaDataEntrada` | data/hora ou nulo | Data da entrada mais recente do item na loja. |

## Transferencias

### `GET /api/transferencias-automaticas`

Lista transferencias automaticas pendentes: `Automatica = '1'`, `statusimpressao = '1'` e `DataAuto IS NULL`. Aceita paginacao opcional.

| Campo | Tipo | Descricao |
|---|---|---|
| `referencia` | texto | Referencia do produto. |
| `CodSecao` | inteiro ou nulo | Codigo da secao. |
| `DescSecao` | texto | Descricao da secao. |
| `codcor` | inteiro ou nulo | Codigo da cor. |
| `DescCor` | texto | Descricao da cor. |
| `CodNumero` | inteiro ou nulo | Codigo da numeracao. |
| `DescNumero` | texto | Descricao da numeracao. |
| `Quant` | inteiro | Quantidade a transferir. |
| `NotaFiscal` | texto | Numero da nota fiscal. |
| `Loja` | texto | Codigo da loja de destino. |

## Controle de volumes

### `GET /api/ControleVolumes`

Retorna os registros da tabela `ControleVolumes`. Nao possui paginacao. Possui timeout de 60 segundos e suporta cancelamento.

Consulta executada:

```sql
SELECT Lote, Descricao, quant, total, data, usuario, Loja, Status
FROM ControleVolumes
```

| Campo |
|---|
| `Lote` |
| `Descricao` |
| `quant` |
| `total` |
| `data` |
| `usuario` |
| `Loja` |
| `Status` |

Os tipos JSON seguem os tipos retornados diretamente pelo SQL Server.

## Vendas

### `GET /api/consulta-vendas`

Retorna as vendas da tabela ou view `ConsultaVenda` dentro do periodo informado. Nao possui paginacao. Possui timeout de 60 segundos e suporta cancelamento.

Exemplo:

```http
GET /api/consulta-vendas?dataInicio=2022-01-01&dataFim=2022-01-31
```

| Campo | Descricao |
|---|---|
| `codcli` | Codigo do cliente. |
| `quant` | Quantidade vendida. |
| `Categoria` | Categoria do produto. |
| `pedido` | Numero ou identificador do pedido. |
| `valor` | Valor unitario registrado. |
| `ValorTotal` | Valor liquido total da linha ou venda, ja com desconto aplicado quando informado pela origem. |
| `DescUnitario` | Desconto unitario. |
| `ValorReal` | Valor informado pela origem para a linha; pode representar valor bruto antes de descontos dependendo da venda. |
| `DescontoVenda` | Desconto aplicado na venda. |
| `data` | Data da venda. |
| `Cor` | Cor do produto. |
| `Numero` | Numeracao ou tamanho. |
| `vendedor` | Vendedor responsavel. |
| `subtotal` | Subtotal da venda. |
| `total` | Total da venda. |
| `desconto` | Desconto geral. |
| `Secao` | Secao do produto. |
| `referencia` | Referencia do produto. |
| `Fornecedor` | Fornecedor do produto. |
| `Grupo` | Grupo do produto. |
| `SubGrupo` | Subgrupo do produto. |
| `loja` | Codigo da loja. |
| `ValorCusto` | Valor de custo do produto. |
| `colecao` | Colecao do produto. |
| `DtProduto` | Data relacionada ao cadastro do produto. |
| `Departamento` | Departamento do produto. |

Os tipos JSON seguem os tipos retornados diretamente pelo SQL Server.

### `GET /api/consulta-vendas-agregado`

Retorna vendas agrupadas diretamente no SQL Server a partir de `ConsultaVenda`. Esse endpoint reduz o volume de retorno para uso em analises, curva ABC e recompra. Possui timeout de 60 segundos, suporta cancelamento e aceita `dataInicio` a partir de `2024-01-01`.

Exemplo:

```http
GET /api/consulta-vendas-agregado?dataInicio=2024-01-01&dataFim=2024-01-31
```

| Campo | Tipo | Descricao |
|---|---|---|
| `data_venda` | data | Data da venda, sem horario. |
| `referencia` | texto | Referencia do produto. |
| `numero` | texto | Numeracao ou tamanho. |
| `loja` | texto | Codigo da loja. |
| `grupo` | texto | Grupo do produto. |
| `subgrupo` | texto | Subgrupo do produto. |
| `fornecedor` | texto | Fornecedor do produto. |
| `qtde_venda` | decimal | Soma da quantidade vendida no grupo, preservando o sinal original da origem. |
| `total_venda` | decimal | Soma do valor liquido da linha, preservando descontos e aplicando o sinal de `quant` quando a devolucao vier com valor positivo na origem. |
| `total_custo` | decimal | Soma do campo `ValorCusto` no grupo. |

Agrupamento aplicado: `data`, `referencia`, `Numero`, `loja`, `Grupo`, `SubGrupo` e `Fornecedor`. Registros com soma de quantidade igual a zero sao removidos. A consulta nao recalcula faturamento por preco de tabela. O faturamento usa o valor liquido da linha, primeiro `ValorTotal`, depois `valor` se `ValorTotal` estiver nulo, e por ultimo `ValorReal - DescontoVenda` se os dois primeiros nao existirem. O sinal de `total_venda` acompanha o sinal de `quant` quando a origem informa devolucao com valor positivo.

Validacao operacional:

```http
GET /api/consulta-vendas-agregado?dataInicio=2026-06-10&dataFim=2026-06-15
```

Ao filtrar o resultado por `fornecedor = TRIFIL`, a soma das linhas filtradas deve retornar:

| Metrica | Valor esperado |
|---|---:|
| `SUM(qtde_venda)` | `200` |
| `SUM(total_venda)` | `10795,12` |

Para devolucoes, `quant` deve permanecer negativo e `total_venda` deve sair negativo mesmo quando `ValorReal` vier positivo na origem. Exemplo: uma devolucao `-1 / 109,90` e uma venda `+1 / 109,90` para a mesma combinacao de agrupamento resultam em quantidade liquida `0` e nao devem inflar `total_venda`; pelo `HAVING`, grupos com quantidade liquida zero nao sao retornados.

Exemplo de desconto por venda: no pedido `8021726-08`, as linhas liquidas `210,54`, `105,27` e `184,19` somam `500,00`, enquanto o subtotal bruto `569,75` e o desconto `69,75` nao devem inflar `total_venda`.

Caso isolado usado para conferencia: fornecedor `TRIFIL`, loja `7`, data `2026-06-15`, referencia `C06244`.

| `numero` | `qtde_venda` esperada | `total_venda` esperado |
|---|---:|---:|
| `50` | `1` | `109,90` |
| `52` | `-1` | `-109,90` |

Conferencia adicional: para a referencia `C06244` no periodo `2026-06-10` a `2026-06-15`, o total esperado e `1637,05`.

## Entradas

### `GET /api/consulta-entradas`

Retorna entradas da tabela `centrada` dentro do periodo informado. A consulta relaciona numeracao, fornecedor, produto, subgrupo, grupo e produto por barras. Registros repetidos conforme a chave definida na consulta sao reduzidos ao registro mais recente.

Nao possui paginacao. Possui timeout de 60 segundos e suporta cancelamento.

Exemplo:

```http
GET /api/consulta-entradas?dataInicio=2022-01-01&dataFim=2022-01-31
```

A resposta inclui:

1. Todas as colunas existentes na tabela `centrada`, devido ao uso de `c.*`.
2. As colunas adicionais abaixo.

| Campo adicional | Descricao |
|---|---|
| `Cor` | Cor obtida de `ProdutoBarras`. |
| `Fornecedor` | Nome do fornecedor. |
| `codSubGrupo` | Codigo do subgrupo do produto. |
| `Venda` | Valor do campo `Venda` do produto. |
| `SubGrupo` | Nome do subgrupo. |
| `Grupo` | Nome do grupo. |
| `numero` | Descricao da numeracao. |
| `Colecao` | Colecao do produto. |
| `Categoria` | Categoria do produto. |
| `rn` | Posicao tecnica da deduplicacao; os registros retornados possuem valor `1`. |

Os nomes e tipos das colunas de `centrada` precisam ser obtidos diretamente do esquema do SQL Server. Os tipos JSON seguem os tipos retornados pelo banco.
Se `centrada` possuir uma coluna com exatamente o mesmo nome de uma coluna adicional, o valor da coluna adicional prevalece no JSON.

### `GET /api/consulta-entradas-agregado`

Retorna entradas agrupadas diretamente no SQL Server a partir de `centrada`, usando os mesmos relacionamentos de produto, fornecedor, grupo, subgrupo e numeracao do endpoint `/api/consulta-entradas`. Possui timeout de 60 segundos, suporta cancelamento e aceita `dataInicio` a partir de `2024-01-01`.

Exemplo:

```http
GET /api/consulta-entradas-agregado?dataInicio=2024-01-01&dataFim=2024-01-31
```

| Campo | Tipo | Descricao |
|---|---|---|
| `data_entrada` | data | Data da entrada, sem horario. |
| `referencia` | texto | Referencia do produto. |
| `numero` | texto | Numeracao ou tamanho. |
| `loja` | texto | Codigo da loja. |
| `grupo` | texto | Grupo do produto. |
| `subgrupo` | texto | Subgrupo do produto. |
| `fornecedor` | texto | Nome do fornecedor. |
| `qtde_entrada` | decimal | Soma da quantidade de entrada no grupo, preservando o sinal original da origem. |
| `valor_entrada` | decimal | Soma de `Quant * Unitario`; quando `Unitario` for nulo, usa `Valor`. Preserva o sinal original de `Quant`. |

Antes da agregacao, a consulta aplica a mesma deduplicacao por `ROW_NUMBER()` usada no endpoint detalhado de entradas. Registros com soma de quantidade igual a zero sao removidos.

## Recebimentos

### `GET /api/recebimentos/crediarista/titulos`

Retorna os titulos de `CReceber` no periodo de vencimento informado, enriquecidos com dados basicos do cliente, cidade e forma de pagamento.

Este endpoint foi pensado para a cobranca de crediaristas/central. Por isso, ele nao retorna titulos com mais de 70 dias de atraso; esses devem ser tratados no fluxo CSC.

Parametros:

| Parametro | Obrigatorio | Descricao |
|---|---|---|
| `dataInicio` | sim | Primeiro vencimento incluido, no formato `YYYY-MM-DD`. |
| `dataFim` | sim | Ultimo vencimento incluido, no formato `YYYY-MM-DD`. |
| `loja` | nao | Filtra por `CReceber.Empresa`. |
| `somenteEmAberto` | nao | Padrao `true`. Quando `true`, aplica `c.Pago IS NULL`. |

O periodo maximo permitido e de 120 dias por requisicao.

Exemplo:

```http
GET /api/recebimentos/crediarista/titulos?dataInicio=2026-08-01&dataFim=2026-08-20&somenteEmAberto=true
```

Campos principais:

| Campo | Descricao |
|---|---|
| `dtVencimento` | Data de vencimento do titulo. |
| `valor` | Valor da parcela. |
| `codCli` | Codigo do cliente. |
| `nomeCliente` | Nome do cliente. |
| `bairro` | Bairro do cliente. |
| `nomeCidade` | Cidade do cliente. |
| `dtNascimento` | Data de nascimento. |
| `sexo` | Homem, Mulher ou Nao informado. |
| `codGrupo` | Grupo do cliente. |
| `limite` | Limite cadastrado. |
| `renda` | Renda cadastrada. |
| `idade` | Idade cadastrada. |
| `lojaCadastro` | Loja do cadastro do cliente. |
| `fone` | Telefone principal. |
| `foneReferencia1` | Telefone de referencia 1. |
| `foneReferencia2` | Telefone de referencia 2. |
| `pedido` | Pedido vinculado. Retornado como texto, pois alguns pedidos podem ser alfanumericos. |
| `dtPedido` | Data do pedido. |
| `parcela` | Numero da parcela. |
| `nParcelas` | Quantidade total de parcelas. |
| `loja` | Loja/empresa do titulo. |
| `pago` | Data/valor do campo `Pago`, quando preenchido. |
| `dtBaixa` | Data de baixa. |
| `codFormaPgt` | Codigo da forma de pagamento. |
| `formaPagamento` | Descricao da forma de pagamento. |

O CRM deve calcular localmente informacoes visuais como dias para vencer, dias em atraso, status `a vencer`, `vence hoje`, `em atraso` e faixas de atraso.

### `GET /api/recebimentos/crediarista/clientes-resumo`

Retorna uma linha por cliente no periodo informado, consolidando os titulos retornaveis pela mesma regra do endpoint de titulos.

Exemplo:

```http
GET /api/recebimentos/crediarista/clientes-resumo?dataInicio=2026-08-01&dataFim=2026-08-20&somenteEmAberto=true
```

Campos principais:

| Campo | Descricao |
|---|---|
| `codCli` | Codigo do cliente. |
| `nomeCliente` | Nome do cliente. |
| `qtdeTitulos` | Quantidade de titulos no periodo. |
| `valorTotal` | Soma dos valores dos titulos. |
| `limite` | Limite cadastrado do cliente. |
| `limiteDisponivel` | `limite - valorTotal`. |
| `primeiroVencimento` | Menor vencimento encontrado. |
| `ultimoVencimento` | Maior vencimento encontrado. |
| `ultimaCompra` | Maior `DtPedido` encontrada. |
| `lojas` | Loja principal encontrada no periodo. |

### `GET /api/contasreceberterceiros/titulos`

Retorna os titulos da tabela `CReceberCob`, usada para cobranca externa/terceiros, como CSC e outras empresas de cobranca. O valor retornado e o valor original do titulo, sem calculo de juros.

Tambem existe o alias:

```http
GET /api/contas-receber-terceiros/titulos
```

Parametros:

| Parametro | Obrigatorio | Descricao |
|---|---|---|
| `dataInicio` | condicional | Primeiro dia incluido no filtro de data. Obrigatorio quando `codCli` nao for informado. |
| `dataFim` | condicional | Ultimo dia incluido no filtro de data. Obrigatorio quando `codCli` nao for informado. |
| `tipoData` | nao | Campo de data usado no filtro. Padrao `vencimento`. Aceita `vencimento`, `envio`, `retorno`, `pedido`, `cadastro`, `dtcadastro`, `cadastro-terceiros`, `data-limite`, `limite`, `devolucao` ou `retirada`. |
| `codCli` | nao | Filtra um cliente especifico. Quando informado, permite consultar sem periodo. |
| `loja` | nao | Filtra por `CReceberCob.Empresa`. |
| `codCobranca` | nao | Filtra por `CReceberCob.CodCobranca`, identificando a cobranca externa/terceiro. |
| `status` | nao | Filtra por `CReceberCob.Status`. |
| `somenteEmAberto` | nao | Padrao `true`. Quando `true`, aplica `CReceberCob.Recebimento IS NULL`. |

O periodo maximo permitido e de 730 dias por requisicao. Para consultar todas as parcelas de um cliente especifico, use `codCli` sem periodo.

Exemplos:

```http
GET /api/contasreceberterceiros/titulos?codCli=479410&somenteEmAberto=true
```

```http
GET /api/contasreceberterceiros/titulos?dataInicio=2026-06-01&dataFim=2026-06-30&tipoData=vencimento&loja=10&codCobranca=401&somenteEmAberto=true
```

Campos principais:

| Campo | Descricao |
|---|---|
| `codCli` | Codigo do cliente. |
| `nomeCliente` | Nome do cliente, quando encontrado no cadastro. |
| `cpf` | CPF registrado em `CReceberCob`. |
| `fone` | Telefone registrado em `CReceberCob`. |
| `foneCadastro` | Telefone principal do cadastro do cliente. |
| `dtVencimento` | Vencimento da parcela. |
| `valor` | Valor original da parcela, sem juros. |
| `pedido` | Pedido vinculado. Retornado como texto. |
| `parcela` | Numero da parcela. |
| `nParcelas` | Quantidade total de parcelas. |
| `loja` | Loja/empresa do titulo (`CReceberCob.Empresa`). |
| `codCobranca` | Codigo da cobranca externa/terceiro. |
| `dtEnvio` | Data de envio para cobranca externa. |
| `dtRetorno` | Data de retorno, quando houver. |
| `status` | Status registrado em `CReceberCob`. |
| `recebimento` | Data de recebimento, quando houver. |
| `seq` | Sequencial do registro em `CReceberCob`. |
| `cReceber` | Sequencial/vinculo com a origem do titulo. |
| `observacao` | Observacao da origem, por exemplo venda a carne. |

### `GET /api/contasreceberterceiros/clientes-resumo`

Retorna uma linha por cliente e por `codCobranca`, consolidando os titulos da cobranca externa/terceiros.

Tambem existe o alias:

```http
GET /api/contas-receber-terceiros/clientes-resumo
```

Aceita os mesmos parametros do endpoint de titulos.

Exemplo:

```http
GET /api/contasreceberterceiros/clientes-resumo?codCli=479410&somenteEmAberto=true
```

Campos principais:

| Campo | Descricao |
|---|---|
| `codCli` | Codigo do cliente. |
| `nomeCliente` | Nome do cliente. |
| `cpf` | CPF encontrado em `CReceberCob`. |
| `fone` | Telefone do cadastro ou telefone encontrado em `CReceberCob`. |
| `codCobranca` | Codigo da cobranca externa/terceiro. |
| `lojaPrincipal` | Menor loja encontrada nos titulos do grupo. |
| `qtdeLojas` | Quantidade de lojas diferentes nos titulos. |
| `qtdeTitulos` | Quantidade de parcelas/titulos. |
| `qtdePedidos` | Quantidade de pedidos diferentes. |
| `valorTotalSemJuros` | Soma de `CReceberCob.Valor`, sem juros. |
| `limite` | Limite cadastrado do cliente. |
| `limiteDisponivel` | `limite - valorTotalSemJuros`, considerando somente os titulos retornados por este endpoint. |
| `primeiroVencimento` | Menor vencimento encontrado. |
| `ultimoVencimento` | Maior vencimento encontrado. |
| `primeiroEnvio` | Menor data de envio para cobranca externa. |
| `ultimoEnvio` | Maior data de envio para cobranca externa. |
| `ultimaCompra` | Maior `DtPedido` encontrada. |
| `qtdeComRecebimento` | Quantidade de titulos com `Recebimento` preenchido. |
| `qtdeComRetorno` | Quantidade de titulos com `DtRetorno` preenchido. |

Para limite disponivel global do CRM, some os abertos de `CReceber` e `CReceberCob` antes de subtrair do limite do cliente.

## Observacoes tecnicas

- `/api/consulta-vendas`, `/api/consulta-entradas`, `/api/estoque-lojas` e `/api/ControleVolumes` carregam todos os registros retornados em memoria antes de responder.
- `/api/consulta-vendas-agregado` e `/api/consulta-entradas-agregado` retornam dados ja agrupados no SQL Server, reduzindo o payload.
- O cancelamento desses endpoints ocorre quando o cliente aborta a requisicao HTTP.
- `/api/estoque-matriz` nao possui timeout explicito de 60 segundos nem cancelamento propagado.
- Os endpoints sem paginacao opcional podem retornar volumes grandes de dados.
- Os endpoints novos de agregacao executam apenas `SELECT`; nao fazem `INSERT`, `UPDATE`, `DELETE`, `ALTER` ou qualquer alteracao no schema.

# Documentacao da API ApiGrupos

## Visao geral

- URL local padrao: `http://localhost:5122`
- Swagger: `/swagger`
- Formato das respostas: JSON, exceto a pagina `/configuracao`.
- A API nao possui autenticacao HTTP.
- As credenciais do SQL Server sao configuradas e mantidas apenas em memoria.

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
| `500` | Erro ao executar ou processar a consulta. |
| `503` | Credenciais do banco ainda nao configuradas. |
| `504` | Consulta de vendas, entradas ou estoque das lojas excedeu 60 segundos. |

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
| `ValorTotal` | Valor total do item ou venda. |
| `DescUnitario` | Desconto unitario. |
| `ValorReal` | Valor efetivo apos ajustes ou descontos. |
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
| `qtde_venda` | decimal | Soma da quantidade vendida no grupo. |
| `total_venda` | decimal | Soma do campo `valor` no grupo. |
| `total_custo` | decimal | Soma do campo `ValorCusto` no grupo. |

Agrupamento aplicado: `data`, `referencia`, `Numero`, `loja`, `Grupo`, `SubGrupo` e `Fornecedor`. Registros com soma de quantidade igual a zero sao removidos.

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
| `qtde_entrada` | decimal | Soma da quantidade de entrada no grupo. |
| `valor_entrada` | decimal | Soma de `Quant * Unitario`; quando `Unitario` for nulo, usa `Valor`. |

Antes da agregacao, a consulta aplica a mesma deduplicacao por `ROW_NUMBER()` usada no endpoint detalhado de entradas. Registros com soma de quantidade igual a zero sao removidos.

## Observacoes tecnicas

- `/api/consulta-vendas`, `/api/consulta-entradas` e `/api/estoque-lojas` carregam todos os registros retornados em memoria antes de responder.
- `/api/consulta-vendas-agregado` e `/api/consulta-entradas-agregado` retornam dados ja agrupados no SQL Server, reduzindo o payload.
- O cancelamento desses endpoints ocorre quando o cliente aborta a requisicao HTTP.
- `/api/estoque-matriz` nao possui timeout explicito de 60 segundos nem cancelamento propagado.
- Os endpoints sem paginacao opcional podem retornar volumes grandes de dados.
- Os endpoints novos de agregacao executam apenas `SELECT`; nao fazem `INSERT`, `UPDATE`, `DELETE`, `ALTER` ou qualquer alteracao no schema.

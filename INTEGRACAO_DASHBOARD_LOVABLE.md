# ApiGrupos — contrato final para a Lovable / Book Lojas

Implementação local de 17/11/2026. **Ainda não publicada e não homologada contra o SQL Server do ERP.** Não há conexão de homologação disponível; a conferência dos nomes, tipos, chaves, cardinalidade e desempenho reais está pendente, conforme autorizado pelo operador. Os contratos abaixo existem no código e no OpenAPI gerado; a homologação pode exigir ajustes antes da implantação.

OpenAPI completo: [docs/openapi.json](docs/openapi.json). Execução e pendências: [VALIDACAO_DASHBOARD.md](VALIDACAO_DASHBOARD.md).

## Transporte, filtros e tipos

- Base pública já utilizada: `https://api.apiaob.com.br`. As rotas novas ficarão disponíveis nessa base somente depois de implantação pelo operador.
- Todas as rotas exigem `X-API-Key`. Consumir pelo backend da Lovable; guardar o segredo no servidor e aplicar ali as permissões por loja. Um filtro `loja` não é autorização.
- GET, resposta 200 em array JSON completo. `[]` significa consulta concluída sem linhas. Erros retornam status HTTP, nunca um array vazio disfarçado.
- Novas rotas `/api/dashboard/*` **não têm paginação**: `page` ou `pageSize` retornam 400. Não se confirmou uma chave real para paginar fatos sem perder/duplicar linhas. Fotografias são consumidas de uma vez, com filtros opcionais de loja. A paginação legada de Subgrupos permanece disponível.
- `dataInicio` e `dataFim`: informar juntas, `YYYY-MM-DD`. Intervalo inclusivo; servidor consulta `>= início AND < dia seguinte ao fim`. Horas informadas são reduzidas ao dia. Mínimo 1753-01-01 e fim máximo 9999-12-30. Datas são de negócio, sem conversão UTC; os timestamps da origem são preservados.
- Eventos: período obrigatório, até **120 dias inclusivos**, permitindo importar anos por janelas. Títulos: período opcional, com o mesmo limite quando informado. Entradas a confirmar e transferências: período opcional sem limite de dias.
- Códigos de loja/origem: inteiro JSON normalizado, por exemplo `"03"` da origem vira `3`. Filtros inteiros não negativos. Códigos de vendedor, produto, seção, subgrupo de transferência, cor e número são texto nos contratos novos. Pedidos, barras, notas e referências são texto; não remover prefixos ou zeros desses campos.
- Abaixo, `int?`, `decimal?`, `texto?`, `data?` e `bool?` aceitam `null`. `decimal` usa aritmética decimal no servidor e é número no JSON. Não substituir nulos financeiros por zero durante importação. Datas JSON são ISO 8601 sem `Z`/deslocamento; não aplicar conversão de fuso ao dia de negócio.
- Timeout SQL: 60 s; cancelamento acompanha a requisição. 400: filtros inválidos; 401: chave ausente/inválida; 503: banco/chave não configurados; 504: timeout; 500: falha de consulta. Não há truncamento silencioso de resultados.

## Rotas e campos finais

### GET /api/dashboard/recebimentos

Filtros: `dataInicio`, `dataFim` obrigatórios; `tipoData=baixa|vencimento` (padrão `baixa`); `loja`, `lojaRecebimento` opcionais.

| Campos | Tipo |
|---|---|
| `codFormaPagamento`, `loja`, `lojaRecebimento` | int? |
| `formaPagamento`, `tipo`, `operador`, `pedido` | texto? |
| `valor`, `vlpago` | decimal? |
| `baixa`, `vencimento` | data? |

Fonte: ContaReceber INNER JOIN FormaPG. Universo fixo `Baixa >= 2022-01-01`, inclusive quando `tipoData=vencimento`. Todas as formas e pedidos, inclusive `REC AG.` e nulos. `loja` = Empresa; `lojaRecebimento` = EmpresaRec. `valor` e `vlpago` têm significados diferentes. Não há JOIN com vendas que multiplique pagamentos.

### GET /api/dashboard/titulos

Filtros opcionais: período; `tipoData=pedido|vencimento|baixa` (padrão `pedido`); `loja`; `situacao=todos|baixa-nula|pago-nulo|pago-preenchido` (padrão `todos`). Sem período, permite fotografia integral.

| Campos | Tipo |
|---|---|
| `codCli`, `parcela`, `nParcelas`, `loja`, `codFormaPgt` | int? |
| `pedido`, `formaPagamento` | texto? |
| `valor` | decimal? |
| `dtVencimento`, `dtPedido`, `dtBaixa` | data? |
| `pago` | bool? |

Fonte: CReceber INNER JOIN FormaPG. Sem corte de 70 dias, sem corte global de DtPedido em 2022, sem deduplicação por pedido/parcela. `baixa-nula` usa Baixa IS NULL; `pago-nulo` usa Pago IS NULL; `pago-preenchido` usa Pago IS NOT NULL, **incluindo false**. A consulta contém apenas o resultado explícito de títulos; o segundo `SELECT *` do PBIX foi descartado deliberadamente.

### GET /api/dashboard/metas-lojas

Filtros opcionais: `ano` (2022..9999), `mes` (1..12), `loja`. Origem desde 2022. Sem DISTINCT nem agregação: preservar todas as linhas aditivas da origem.

| Campos | Tipo |
|---|---|
| `loja`, `mes` | int? |
| `ano` | int |
| `data` | data? (primeiro dia do mês) |
| `vendas`, `avista`, `receber`, `terceiros`, `valorRM`, `receberMaisTerceiros` | decimal? |

`receberMaisTerceiros = receber + terceiros`, preservando null. Seq/IdEmpresa/IdFilial não foram presumidos como chaves e não são expostos nesta versão. Não fazer upsert por loja/mês, pois isso pode sobrescrever metas aditivas; substituir a partição importada inteira. Não há meta trimestral inventada.

### GET /api/dashboard/metas-vendedores

Filtros opcionais: `ano` (2022..9999), `mes` (1..12), `loja`. Origem desde 2022.

| Campos | Tipo |
|---|---|
| `loja`, `mes` | int? |
| `ano` | int |
| `codVend`, `vendedor` | texto? |
| `data` | data? (primeiro dia do mês) |
| `vendas` | decimal? |

JOIN original Vendedores.codvend = MetasVendedor.CodVend. A loja retornada é a da meta; não se exige igualdade com a loja atual do vendedor. Não juntar e somar metas repetidas por item de venda.

### GET /api/dashboard/vendedores

Filtros opcionais: `loja`, `codVend` (texto exato de 1..100 caracteres).

Campos: `codVend: texto?`, `vendedor: texto?`, `loja: int?`. Sem filtro de inativos. Não retorna PIS nem outros dados pessoais do cadastro. Não usar o nome como chave: podem existir homônimos; a unicidade do código ainda precisa ser conferida no ERP.

### GET /api/dashboard/entradas-a-confirmar

Filtros opcionais: `loja`, período. Omitir datas para obter **todas** as pendências existentes, inclusive antigas.

| Campos | Tipo |
|---|---|
| `loja` | int? |
| `quant`, `valor`, `valorTotal` | decimal? |
| `barras`, `notaFiscal`, `barrasProduto`, `grupo`, `subGrupo`, `fornecedor`, `referencia`, `secao`, `numero`, `cor` | texto? |
| `data` | data? |

Fonte: EntradaControle INNER JOIN ProdutoBarras por Barras, sem filtro de status inventado. `valorTotal = quant * valor`. Valores e quantidades negativos preservados. O mecanismo real de encerramento/remoção em EntradaControle precisa ser confirmado pelo operador. A API entrega cada linha uma vez; a dupla soma da cobertura no DAX não foi introduzida nos fatos.

### GET /api/dashboard/transferencias

Filtros opcionais: `origem`, `loja` (destino), período por DataTransferencia, `tipoTransferencia`, `somentePendentes` (padrão false). Omitir período para fotografia completa dentro dos horizontes originais.

| Campos | Tipo |
|---|---|
| `referencia`, `codSecao`, `descSecao`, `codSubGrupo`, `descSubGrupo`, `codCor`, `descCor`, `codNumero`, `descNumero`, `codFor`, `nomeFornecedor`, `notaFiscal`, `usuarioAuto`, `usuarioConfirma`, `tipoTransferencia`, `origemRegistro`, `grupo` | texto? |
| `quant` | decimal? |
| `loja`, `origem` | int? |
| `statusImpressao`, `automatica` | bool? |
| `dataTransferencia`, `dataAuto` | data? |
| `tipo`, `status` | texto? calculado |
| `produtoCompleto` | texto calculado |

Dois ramos, preservados com UNION ALL:

1. Transferencia desde 2024-01-01: Automatica=1 e UsuarioAuto não nulo → `AUTOMATICA AUTORIZADA`; Automatica=1 e UsuarioAuto nulo → `AUTOMATICA PENDENTE`; demais → `MANUAL`.
2. TransferenciaControle de origem 5 desde 2026-01-01, com LEFT JOIN pela referência, cor, número, nota, origem e destino: UsuarioConfirma não nulo → `MANUAL EFETIVADA`; demais → `MANUAL PENDENTE`. Automatica=false; DataAuto e statusImpressao nulos.

`tipoTransferencia` aceita exatamente essas cinco categorias (entrada sem diferenciar maiúsculas). `somentePendentes=true` seleciona as duas categorias PENDENTE, em interseção com os demais filtros.

`tipo`: true → Automática; false → Manual; null → null. `status`: statusImpressao=true → Preparado Para Envio; false → Não Preparado; null e MANUAL PENDENTE → Preparado Para Envio; demais → null. `produtoCompleto` junta descSubGrupo, descSecao, descNumero e descCor com ` - `, ignorando partes null e preservando strings vazias. Alias original `nome_for` → `nomeFornecedor`. `grupo` vem de Produtos.Grupo → Grupos; não é departamento.

Multiplicidade dos JOINs originais por referência e controle foi preservada; não foi feita deduplicação arbitrária. Conferir as contagens do script de esquema antes de somar em produção. UsuarioAuto, DataAuto e UsuarioConfirma são campos independentes. Não se aplica o corte de junho/2026 da rota legada.

### GET /api/dashboard/ativacoes

Filtros: período obrigatório até 120 dias; `loja` opcional.

Campos: `codCli: int?`, `dtPrimeiraCompra: data?`, `loja: int?`, `valor: decimal?`, `pedido: texto?`, `dtCadastro: data?`, `vendedor: texto?`, `quant: int`.

MIN(data) por cliente é calculado em toda a história antes de restringir o período. Todos os pedidos na primeira data/hora participam do agrupamento original por cliente, loja, total, pedido, cadastro e vendedor. Exclui cliente 1376401. `quant=1` quando valor>0, senão 0. Dois pedidos na primeira data podem gerar duas linhas; não é DISTINCTCOUNT de clientes. Nome do cliente foi omitido do payload, mantendo-se no agrupamento do SQL original.

### GET /api/dashboard/referencias

Filtro opcional: `referencia`, comparação SQL exata conforme a collation do ERP (1..200 caracteres). Não faz busca por prefixo nem remove `26-`. Sem período e sem corte de cadastro em 2025.

Campos: `referencia: texto?`, `custo: decimal?`, `codSecao: texto?`, `descricao: texto?`, `codProd: texto?`, `fornecedor: texto?`, `grupo: texto?`, `subGrupo: texto?`, `codCategoria: int?`, `categoria: texto?`.

Preserva INNER JOINs com grupo, seção, subgrupo e fornecedor e LEFT JOIN em categoria. Escolhe o menor Produtos.codigo por referência **dentro do conjunto elegível desses JOINs**. `descricao` vem de Secao.Descricao. Eliminou-se apenas o MAX/GROUP BY externo redundante após rn=1; não se trocou a origem da descrição. Cadastros sem as dimensões exigidas continuam fora, como na fonte original.

### GET /api/dashboard/terceiros

Filtros: período obrigatório até 120 dias por vencimento; `loja`, `status` opcionais. Para inadimplência usar `status=0`.

Campos: `codCli: int?`, `vencimento: data?`, `valor: decimal?`, `pedido: texto?`, `dtPedido: data?`, `parcela: int?`, `nParcelas: int?`, `codCobranca: int?`, `nomeCobranca: texto?`, `loja: int?`, `status: int?`.

Fonte CReceberCob INNER JOIN CobrancaTerceiros. Sem condição Recebimento IS NULL. Contrato separado da rota de cobrança existente, com o universo do PBIX e sem dados pessoais de contato.

### GET /api/dashboard/vendas-agregado

Filtros: período obrigatório até 120 dias; `loja` opcional. Não limita início a 2024.

Campos: `dataVenda: data?`, `referencia: texto?`, `numero: texto?`, `loja: int?`, `grupo: texto?`, `subGrupo: texto?`, `fornecedor: texto?`, `quantidade: decimal?`, `faturamentoPbix: decimal?`, `custoTotalPbix: decimal?`.

Granularidade: dia/referência/número/loja/grupo/subgrupo/fornecedor. `quantidade=SUM(quant)`, `faturamentoPbix=SUM(ValorTotal)`, `custoTotalPbix=SUM(quant*ValorCusto)`, sem fallback, ajuste de sinal ou HAVING quantidade<>0. Margem e percentuais ficam no backend analítico. Para vendedor/cliente/pedido, continuar usando vendas detalhadas.

### GET /api/dashboard/entradas-agregado

Filtros: período obrigatório até 120 dias; `loja` opcional. Universo desde 2022-01-01.

Campos: `dataEntrada: data?`, `referencia: texto?`, `numero: texto?`, `loja: int?`, `grupo: texto?`, `subGrupo: texto?`, `fornecedor: texto?`, `quantidade: decimal?`, `valorVendaEntradaPbix: decimal?`, `custoTotalEntradaPbix: decimal?`.

Deduplicação sobre **todo o horizonte desde 2022 antes do filtro de período**: NotaFiscal, Referencia, Loja, número, fornecedor, código de subgrupo, Venda, nome de subgrupo, grupo, Colecao, Categoria, barras; mantém a maior Data. Depois agrupa por dia/referência/número/loja/grupo/subgrupo/fornecedor. `quantidade=SUM(Quant)`, venda=SUM(Quant*Venda), custo=SUM(Quant*Valor). Sem JOIN adicional em ProdutoBarras. Empates na maior Data conservam a indeterminação do original; requerem conferência no ERP antes de definir desempate por chave real. Importações incrementais precisam recompor partições antigas quando surgirem registros mais recentes da mesma partição.

### GET /api/Subgrupos e /api/Subgrupos/por-grupo/{grupoCodigo}

Preservados array sem paginação e envelope `{items,page,pageSize,total,totalPages}` quando `page`/`pageSize` informados, com padrões/limites legados. Campos finais:

`id: int`, `nome: texto`, `grupoCodigo: int?`, `codNcm: texto?`, `departamento: texto?`, **`departamentoCodigo: int?`**.

Única ampliação de payload legado: departamentoCodigo = **Subgrupos.grupo**, pendente de confirmação no esquema real. 0=eletro, 1=móveis, 2=confecções. `departamento` continua sendo o texto de Grupos.nomegrupo e `grupoCodigo` continua Subgrupos.CodGrupo. Corrigidos aliases ambíguos nas duas consultas paginadas; todas as contagens usam o mesmo JOIN dos itens. Ordem por nome e código; a unicidade real dessa combinação após JOIN ainda precisa de conferência. O cálculo do intervalo de páginas agora usa inteiro de 64 bits para evitar overflow.

## Mapa indicador → endpoint → campos → filtros → transformação

| Indicador | Endpoint final | Campos | Filtros / transformação no backend Lovable |
|---|---|---|---|
| Faturamento, custo e margem | `/api/dashboard/vendas-agregado` | faturamentoPbix, custoTotalPbix, quantidade | Período; somar; margem conforme DAX. Para pedido/vendedor usar `/api/consulta-vendas`. |
| Venda/meta e metas proporcionais | `/api/dashboard/metas-lojas` + vendas | vendas, data, loja | Ano/mês; somar linhas aditivas; aplicar calendário/feriados do dashboard. |
| Recebimentos totais / YOY | `/api/dashboard/recebimentos` | vlpago, pedido, baixa, loja | tipoData=baixa; aplicar exclusões exatas por LEFT do DAX; período LY em outra chamada. |
| Capitalização / YOY | `/api/dashboard/recebimentos` | vlpago, formaPagamento, pedido | 19 formas abaixo; excluir apenas pedido igual a REC AG.; nulos elegíveis. |
| Capitalização por vendedor | recebimentos + `/api/consulta-vendas` | pedido, loja, vlpago; vendedor | Semijoin por pares distintos (pedido,loja) das vendas no contexto; preservar a data de baixa. |
| Recebimentos + terceiros | `/api/dashboard/recebimentos` | vlpago, formaPagamento, pedido | Recebimentos menos capitalização base; não confundir com CReceberCob. |
| Meta de capitalização | `/api/dashboard/metas-lojas` | avista | Somar por loja/mês. |
| Meta de recebimentos + terceiros / total | `/api/dashboard/metas-lojas` | receber, terceiros, avista | receber+terceiros; total acrescenta avista. |
| Aberto integral | `/api/dashboard/titulos` | valor, dtVencimento | tipoData=vencimento, situacao=baixa-nula; para toda a fotografia omitir datas. |
| Inadimplência | títulos + recebimentos + `/api/dashboard/terceiros` | valor, dtVencimento/vencimento, formaPagamento, status | Aberto=Baixa nula; terceiros=status 0; recebido elegível=valor das formas CARTÃO AO BARULHO/COBRANÇA EXTERNA por vencimento. Total=aberto+terceiros+recebido; inad=(total−recebido)/total. |
| Clientes únicos cartão | `/api/dashboard/titulos` | codCli, formaPagamento, dtPedido | tipoData=pedido, forma CARTÃO AO BARULHO no cliente; DISTINCTCOUNT no período inteiro, não somar contagens mensais. |
| Meta/atingimento vendedor | metas-vendedores + vendedores + vendas detalhadas | codVend, vendedor, loja, data, vendas | Somar meta uma vez por linha de meta; não repetir por item vendido; conferir correspondência código/nome. |
| Ativações | `/api/dashboard/ativacoes` | vendedor, loja, quant, pedido, dtPrimeiraCompra | Somar quant; primeira compra histórica já tratada no ERP. |
| Estoque atual/antigo/inverno | `/api/estoque-lojas` + referências | quant, valor, ultimaDataEntrada, colecao; custo/categoria/seção | Fotografia, incluindo negativos e loja 5; conferir campos legados no OpenAPI/documentação. |
| Entradas: valor de venda e custo | `/api/dashboard/entradas-agregado` | valorVendaEntradaPbix, custoTotalEntradaPbix | Somar após deduplicação global; sem usar valor_entrada do agregado legado como substituto. |
| Vestuário em trânsito / cobertura | `/api/dashboard/entradas-a-confirmar` + estoque + metas-lojas | valorTotal, grupo; valorRM | Sem corte de data para fotografia; grupos de vestuário abaixo; estoqueIdeal=metaRM×4. Resolver a dupla soma do DAX na homologação analítica. |
| Produtos pendentes / preparo | `/api/dashboard/transferencias` | tipoTransferencia, status, produtoCompleto, quant, grupo | origem=5, somentePendentes=true; cliente seleciona INFORMATICA, MOVEIS, PORTATEIS, ELETRO ELETRONICOS. |
| Departamento / ramo duro e mole | `/api/Subgrupos` | id, departamentoCodigo | 0 eletro, 1 móveis, 2 confecções; duro=0+1, mole=2; não usar grupoCodigo como departamento. |
| Referências antigas | `/api/dashboard/referencias` | referencia, codProd, custo, codSecao, descricao, codCategoria, categoria | Sem corte 2025; menor código elegível, com descrição da seção. |

Recebimentos: excluir pedido igual a `REC AG.`; aplicar literalmente `LEFT(pedido,9) = "R Rec AG."`, `LEFT(pedido,6) = "R R:AC"`, `LEFT(pedido,4) = "R:AC"` como condições de exclusão, respeitando a semântica de comparação do DAX (inclusive espaços). Não transformar em uma busca genérica por prefixo sem homologar.

As 19 formas de capitalização, preservando espaços e acentos:

```json
["PIX","CHEQUE A VISTA"," DINHEIRO","MAQ STONE 02-06X","MAQ CIELO  2-10X","MAQ STONE 07-12X","MAQ STONE 02-06X ELO","MAQ STONE CRED ELO","MAQ STONE CREDITO","MAQ STONE DEBITO","MAQ STONE DEBITO ELO","ENTRADA","CARTAO ECOMMERCE","CONVENIOS CARD","MAQ STONE 07-12X ELO","VALECON","SOROCRED 01-07X","CHEQUE PRÉ","MAQ STONE PIX"]
```

Grupos de vestuário: ACESSORIO ADULTO, ACESSORIOS INFANTIL, CALCADOS ADULTO, CALCADOS INFANTIL, COSMETICOS, CONFECCOES ADULTO, CONFECCOES INFANTIL, CONFECCOES BEBE. Aplicar seleções no backend; o endpoint não exclui outros grupos.

## Exemplo sintético

```http
GET /api/dashboard/recebimentos?dataInicio=2026-06-10&dataFim=2026-06-15&tipoData=baixa&loja=3
X-API-Key: <SEGREDO_NO_BACKEND>
```

```json
[
  {
    "codFormaPagamento": 999,
    "formaPagamento": "MAQ STONE PIX",
    "tipo": "EXEMPLO",
    "operador": null,
    "valor": 120.00,
    "vlpago": 100.00,
    "baixa": "2026-06-15T15:00:00",
    "loja": 3,
    "lojaRecebimento": 5,
    "vencimento": "2026-06-10T00:00:00",
    "pedido": "EXEMPLO-001"
  }
]
```

Os códigos e valores acima são fictícios. Mais requests prontos: [ApiGrupos.http](ApiGrupos.http).

## Compatibilidade mantida

Não mudaram as fórmulas/filtros das rotas atuais de vendas, estoque, entradas, crediarista, terceiros e produtos. A capitalização legada permanece com seu universo anterior de 17 formas e limite de 120 dias; **o novo dashboard deve usar `/api/dashboard/recebimentos` e as 19 formas**, não aquela rota. O corte de 70 dias da cobrança e o corte da rota de transferências existente permanecem. Nenhuma tabela, registro ou view do ERP foi alterada e nenhum serviço foi publicado/substituído.

Rankings, metas proporcionais, feriados, RLS, calendário e percentuais são responsabilidade do backend analítico. Os novos endpoints entregam fatos e cálculos de linha, não executam o grafo DAX inteiro. Sem chaves de origem comprovadas, importar janelas/fotografias como partições substituíveis, preservando a multiplicidade de linhas; não deduplicar por pedido/parcela nem por loja/mês por suposição.

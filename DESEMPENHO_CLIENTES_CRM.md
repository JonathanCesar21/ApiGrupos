# Comparacao de desempenho de clientes CRM

Medicao realizada em 19/10/2026, antes da implementacao do historico e apos a instalacao da versao 1.3.3 no computador da API. A nova resposta foi confirmada pela presenca de `historico` antes de iniciar as amostras.

Endpoint: `https://api.apiaob.com.br/api/clientes/crm`, sem filtro de cliente ou loja. Todas as consultas completas percorreram 188.274 clientes em duas paginas, com `pageSize=150000`. Cada fase executou um aquecimento completo, excluido das estatisticas, seguido de tres consultas completas sequenciais. Foi usado o mesmo computador e script de medicao.

| Medida da consulta completa | Antes | Depois | Variacao |
|---|---:|---:|---:|
| Clientes retornados | 188.274 | 188.274 | 0 |
| Paginas | 2 | 2 | 0 |
| Mediana HTTP | 103,98 s | 98,41 s | -5,36% |
| Mediana total do script, incluindo leitura do JSON | 176,09 s | 213,79 s | +21,41% |
| Mediana do tamanho do JSON UTF-8 | 129,88 MB | 233,20 MB | +79,55% |

O tempo HTTP e a soma das requisicoes das duas paginas e inclui execucao no servidor, serializacao e transferencia ate o cliente de teste. A leitura e validacao do JSON no PowerShell ficam fora dessa medida HTTP, mas entram no tempo total do script. Os tamanhos usam MB decimais e representam o conteudo JSON, sem medir bytes de TLS ou cabecalhos.

| Fase | Amostra | HTTP completo | Total do script | JSON |
|---|---:|---:|---:|---:|
| Antes | 1 | 103,98 s | 199,86 s | 129,88 MB |
| Antes | 2 | 85,36 s | 176,09 s | 129,88 MB |
| Antes | 3 | 111,04 s | 162,21 s | 129,88 MB |
| Depois | 1 | 98,41 s | 223,41 s | 233,20 MB |
| Depois | 2 | 92,61 s | 204,63 s | 233,20 MB |
| Depois | 3 | 138,33 s | 213,79 s | 233,20 MB |

Nao houve aumento na mediana HTTP observada. As faixas se sobrepoem e houve uma amostra posterior de 138,33 s; a queda de 5,36% na mediana nao demonstra uma melhoria do tempo SQL isolado. Rede, uso concorrente do servidor, caches e recursos do computador de teste podem variar entre as fases. A base estava em uso; a contagem de clientes foi conferida, sem snapshot transacional entre as consultas.

O impacto confirmado foi o aumento de aproximadamente 103,32 MB no JSON por consulta completa e de 37,70 s na mediana total do script. O custo do processamento SQL nao foi isolado. O tempo de leitura do PowerShell nao deve ser usado como estimativa direta para um navegador ou outro consumidor da API.

A recomendacao e manter os indicadores para analises que precisam do historico. Para consultas que dispensam esses campos, usar `incluirHistorico=false` preserva a consulta anterior e omite o objeto adicional. Nenhum indicador foi removido por causa desta medicao.

Reproducao: `scripts/medir-clientes-crm.ps1 -Fase Antes -BaseUrl https://api.apiaob.com.br/` antes da atualizacao, seguido do mesmo comando com `-Fase Depois` apos atualizar. A fase Antes existente e preservada. Para outra linha de base, definir outro `-DiretorioResultados`. A chave e lida do ambiente ou `.env` local, ignorado pelo Git, e nao e registrada nos resultados.

Arquivos brutos locais, ignorados pelo Git: `build-check/crm-benchmark/antes.json`, `depois.json` e `comparacao.json`. Esses arquivos guardam tempos, tamanhos e contagens, sem nomes, telefones ou registros individuais.

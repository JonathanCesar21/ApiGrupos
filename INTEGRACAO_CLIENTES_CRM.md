# Atualizacao da integracao de clientes CRM

Pacote 1.3.5, de 22/10/2026. Inclui o campo `celular` do cadastro no retorno e na busca do CRM. Este guia tambem consolida o contrato dos indicadores implementados e validados na versao 1.3.3, a data de cadastro do cliente e os resultados da medicao de desempenho. Os calculos de historico permanecem os mesmos que foram medidos.

O endpoint utilizado pelo CRM e `GET https://api.apiaob.com.br/api/clientes/crm`. A autenticacao usa o header `X-API-Key`. A resposta continua paginada, com `page`, `pageSize`, `total`, `totalPages` e `items`.

## Mudancas para o CRM

1. Ler `items[].dtCadastro` como texto no formato `DD/MM/AAAA`, por exemplo `19/10/2026`. A data e do cadastro do cliente, sem horario, e pode ser `null`. Interpretar dia, mes e ano explicitamente; esse campo nao e uma data ISO.
2. Ler os novos indicadores em `items[].historico`. Os totais atuais de parcelas em aberto, como `qtdEmAbertoTerceiros` e `totalEmAbertoTerceiros`, permanecem no objeto principal do cliente.
3. Usar o novo parametro opcional `incluirHistorico`. O padrao e `true`; com `false`, o objeto `historico` e omitido e os novos calculos nao sao executados.
4. Tratar valores `null` como informacao indisponivel ou insuficiente. O percentual e informado na escala de 0 a 100: `25` representa 25%, nao 0,25.
5. Ler `items[].celular` como texto ou `null`, diretamente de `Clientes.celular`. O campo e retornado com ou sem historico e participa da busca por telefone, por exemplo `?busca=17999990000`.

Consulta de um cliente com historico:

```http
GET https://api.apiaob.com.br/api/clientes/crm?codCli=2021001&incluirHistorico=true&page=1&pageSize=100
X-API-Key: SUA_API_KEY
Accept: application/json
```

Consulta do mesmo cliente sem os novos calculos:

```http
GET https://api.apiaob.com.br/api/clientes/crm?codCli=2021001&incluirHistorico=false
X-API-Key: SUA_API_KEY
Accept: application/json
```

Sem `codCli`, a consulta retorna os clientes conforme os demais filtros. Para sincronizar a base, percorrer as paginas ate `totalPages`. O `pageSize` padrao e 100 e o maximo e 150000.

## Campos de historico

Todos os nomes abaixo sao relativos a `items[].historico`. As quantidades sao numeros inteiros. Medias e percentuais sao numeros JSON, com arredondamento a duas casas decimais; a formatacao visual fica a cargo do CRM.

| Campo | Tipo | Interpretacao |
|---|---|---|
| `qtdEncaminhamentosHistorico` | inteiro | Total de pedidos distintos de acordo ou terceiros por cliente. |
| `qtdAcordosHistorico` | inteiro | Pedidos distintos iniciados por `AC`, pagos ou em aberto. |
| `qtdAcordosEmAberto` | inteiro | Acordos com pelo menos uma parcela em aberto. |
| `qtdAcordosQuitados` | inteiro | Acordos sem parcelas em aberto nas fontes consultadas. |
| `qtdEncaminhamentosTerceirosHistorico` | inteiro | Pedidos distintos que constam em terceiros, pagos ou em aberto. |
| `qtdEncaminhamentosTerceirosEmAberto` | inteiro | Pedidos de terceiros com pelo menos uma parcela sem recebimento. |
| `qtdEncaminhamentosTerceirosQuitados` | inteiro | Pedidos de terceiros com todas as parcelas dessa fonte recebidas. |
| `qtdParcelasPagas12Meses` | inteiro | Quantidade de parcelas pagas dentro da janela dos ultimos 12 meses. |
| `qtdParcelasPagasComAtraso12Meses` | inteiro | Quantidade dessas parcelas paga em dia posterior ao vencimento. |
| `qtdParcelasPagasSemVencimento12Meses` | inteiro | Pagamentos sem vencimento informado, cujo atraso nao pode ser classificado. |
| `mediaDiasAtraso12Meses` | numero ou `null` | Media dos dias de atraso somente entre as parcelas pagas com atraso. |
| `percentualParcelasPagasComAtraso12Meses` | numero ou `null` | `100 * parcelas pagas com atraso / parcelas pagas`. |
| `diaHabitualPagamento` | inteiro de 1 a 31 ou `null` | Dia central da faixa em que o cliente costuma pagar. |
| `concentracaoDiaHabitualPagamento` | numero ou `null` | Percentual ponderado de pagamentos na faixa de dois dias antes/depois do dia estimado. |
| `qtdDatasPagamento12Meses` | inteiro | Quantidade de datas distintas usadas na estimativa do habito. |
| `qtdMesesComPagamento12Meses` | inteiro | Quantidade de meses com pagamento dentro da janela analisada. |

## Regras para exibir e usar os indicadores

Encaminhamentos sao contados por pedido, nao por parcela, considerando todo o historico preservado no banco. Pedidos iniciados por `AC` representam acordos. Acordos antigos ja pagos e acordos ainda em aberto entram nas contagens. Um acordo que tambem esta em terceiros aparece nas duas categorias, mas uma vez no total de encaminhamentos; portanto, nao somar `qtdAcordosHistorico` e `qtdEncaminhamentosTerceirosHistorico` para obter o total.

Os indicadores de pagamento usam a data da baixa no crediario e a data de recebimento em terceiros para selecionar os ultimos 12 meses, incluindo o dia atual do SQL Server. Atraso compara essa data com o vencimento em dias de calendario. Pagamento no mesmo dia do vencimento conta como pontual, independentemente do horario. Conforme a regra do ERP, as parcelas transferidas nao permanecem no crediario e podem ser reunidas sem duplicar parcelas entre as duas fontes.

Por exemplo, 20 parcelas pagas, sendo cinco com atraso e 90 dias de atraso somados, produzem media de 18 dias e percentual de 25%. Sem pagamentos no periodo, media e percentual sao `null`. Com pagamentos conhecidos e nenhum atraso, ambos sao zero. Havendo vencimentos ausentes, o percentual e `null`; a media continua considerando os atrasos conhecidos, quando existirem. A quantidade sem vencimento permite identificar essa situacao no CRM.

O dia habitual considera cada data de pagamento uma vez, equilibra o peso dos meses e da mais peso aos meses recentes. Sao exigidos pelo menos tres meses com pagamentos e concentracao de 60% na faixa de mais ou menos dois dias. Se o padrao estiver disperso, `diaHabitualPagamento` sera `null`. Se houver menos de tres meses, dia e concentracao serao `null`. A concentracao descreve o habito observado, sem comprovar a data de recebimento de salario.

O dia 31 tambem representa pagamentos no ultimo dia de meses mais curtos. Ao montar uma data para contato, usar `min(diaHabitualPagamento, quantidadeDeDiasDoMes)`. A estimativa considera a proximidade entre o fim e o inicio do mes.

Na sincronizacao, a ausencia de `historico` com `incluirHistorico=false` significa que ele nao foi consultado. Nessa situacao, nao substituir um historico previamente armazenado por zeros. Quando o objeto vier incluido, atualizar os valores recebidos, inclusive zeros e `null`, para nao conservar indicadores antigos fora da janela atual. Na interface, valores `null` podem ser exibidos como "Sem dados suficientes", em vez de zero.

## Exemplo de contrato

Exemplo ficticio e parcial de uma resposta, apenas para orientar a integracao. Os valores abaixo nao correspondem ao cliente 2021001.

```json
{
  "page": 1,
  "pageSize": 100,
  "total": 1,
  "totalPages": 1,
  "items": [
    {
      "codigo": 12345,
      "nome": "Cliente de exemplo",
      "dtCadastro": "19/10/2026",
      "celular": "17999990000",
      "qtdEmAbertoTerceiros": 4,
      "totalEmAbertoTerceiros": 800.00,
      "historico": {
        "qtdEncaminhamentosHistorico": 6,
        "qtdAcordosHistorico": 3,
        "qtdAcordosEmAberto": 1,
        "qtdAcordosQuitados": 2,
        "qtdEncaminhamentosTerceirosHistorico": 4,
        "qtdEncaminhamentosTerceirosEmAberto": 1,
        "qtdEncaminhamentosTerceirosQuitados": 3,
        "qtdParcelasPagas12Meses": 20,
        "qtdParcelasPagasComAtraso12Meses": 5,
        "qtdParcelasPagasSemVencimento12Meses": 0,
        "mediaDiasAtraso12Meses": 18.00,
        "percentualParcelasPagasComAtraso12Meses": 25.00,
        "diaHabitualPagamento": 10,
        "concentracaoDiaHabitualPagamento": 80.00,
        "qtdDatasPagamento12Meses": 12,
        "qtdMesesComPagamento12Meses": 8
      }
    }
  ]
}
```

## Desempenho validado

Em 19/10/2026, a consulta completa de 188.274 clientes, em duas paginas, teve mediana HTTP de 103,98 s antes e 98,41 s depois da inclusao do historico. O JSON passou de 129,88 MB para 233,20 MB, e a mediana do script incluindo a leitura do JSON passou de 176,09 s para 213,79 s. A medicao HTTP inclui rede e transferencia, sem isolar o tempo SQL.

O CRM pode solicitar `incluirHistorico=false` quando precisar apenas dos dados basicos e saldos atuais. Para carregar os indicadores comportamentais, solicitar o historico e tratar o maior volume de dados por paginacao.

Contrato completo em [DOCUMENTACAO_API.md](DOCUMENTACAO_API.md). Regras e amostras da medicao em [DESEMPENHO_CLIENTES_CRM.md](DESEMPENHO_CLIENTES_CRM.md). Os tres documentos acompanham o instalador 1.3.5.

# Consulta de transferencia automatica de uma venda

`/api/consulta-vendas` usa a consulta original por periodo. A verificacao de transferencia automatica e o codigo de barras foram movidos para uma rota separada, consultada quando o sistema precisar dessas informacoes.

## Requisicao

```http
GET /api/consulta-vendas-transferencias-automaticas?loja=03&pedido=8008012-03
Accept: application/json
X-API-Key: SUA_API_KEY
```

Use a mesma URL base e API key dos outros endpoints. Nao e necessario informar datas.

| Parametro | Regra |
|---|---|
| `loja` | Obrigatorio. Codigo positivo da loja da venda. `3` e `03` consultam a mesma loja. |
| `pedido` | Obrigatorio. Numero completo, incluindo o sufixo, por exemplo `8008012-03`. Maximo de 100 caracteres. |

## Resposta

```json
{
  "loja": "03",
  "pedido": "8008012-03",
  "TemTransferenciaAutomatica": true,
  "itens": [
    {
      "referencia": "010037LP",
      "Cor": "MALBEC",
      "Numero": "0",
      "barras": "0519365747",
      "quant": 1,
      "TransferenciaAutomatica": 1,
      "LojaOrigemEstoque": "05"
    }
  ]
}
```

- `TemTransferenciaAutomatica`: booleano. `true` se algum produto da venda tiver atendimento automatico pelo deposito. `false` se nenhum item tiver o vinculo.
- `itens`: todos os itens da venda. Cada um tem `TransferenciaAutomatica` inteiro, sempre `0` ou `1`.
- `LojaOrigemEstoque`: `"05"` para item automatico; `null` para origem nao identificada.
- `barras`: texto, preservando zeros a esquerda. `null` quando nao ha um codigo preenchido ou existem varios codigos distintos para a mesma referencia, cor e tamanho.
- `quant`: quantidade do item na venda. As linhas originais sao preservadas, sem agrupamento.

## Como usar no outro sistema

1. Consulte `/api/consulta-vendas` normalmente para obter as vendas.
2. Quando precisar identificar atendimento pelo deposito, chame a nova rota com `loja` e `pedido` da venda.
3. Para saber se a venda tem algum produto automatico, use `TemTransferenciaAutomatica`.
4. Para retirar apenas os produtos ja atendidos pelo deposito da lista de pedidos, use `itens[].TransferenciaAutomatica = 1`. Uma venda pode conter itens com `1` e com `0`; o indicador geral nao deve ser aplicado a todos os produtos.
5. Para ligar a resposta aos itens locais, use loja + pedido + referencia + Cor + Numero. Normalize somente o codigo da loja como numero para comparar `3` com `"03"`; preserve os zeros das referencias, tamanhos e barras.
6. Itens com `TransferenciaAutomatica = 0` continuam no fluxo normal, incluindo a verificacao de estoque para o aviso "Tem na loja".

Uma venda existente sem atendimento automatico retorna `200`, `TemTransferenciaAutomatica = false`, todos os itens com `TransferenciaAutomatica = 0` e `LojaOrigemEstoque = null`.

## Erros

| Status | Significado |
|---|---|
| `400` | Loja/pedido ausentes ou invalidos. |
| `401` | API key ausente ou invalida. |
| `404` | Nenhuma venda encontrada para a loja e o pedido. |
| `500` | Falha ao consultar o banco ou processar a resposta. |
| `503` | Credenciais SQL ou API key do servidor nao configuradas. |
| `504` | Timeout da consulta. |

Verifique o status HTTP antes de ler o resultado. Um erro ou venda nao encontrada nao equivale a `TemTransferenciaAutomatica = false`.

## Regra de identificacao e casos conferidos

A origem e o historico de `Transferencia`, com `Automatica = '1'`, origem 5 e destino igual a loja da venda. O vinculo compara pedido (`NotaFiscal`), referencia, cor e tamanho. Impressao e `DataAuto` nao restringem a consulta: sao considerados itens antes da impressao, impressos ainda pendentes e ja expedidos.

Os dados enviados do ERP confirmaram os vinculos e os seguintes codigos unicos no cadastro:

| Loja | Pedido | Situacao informada | `barras` |
|---|---|---|---|
| `17` | `8008331-17` | Aguardando impressao e efetivacao. | `0519719008` |
| `11` | `8014276-11` | Impresso, aguardando efetivacao. | `0519648832` |
| `03` | `8008012-03` | Impresso e entregue. | `0519365747` |

Para os tres itens, o resultado esperado e `TransferenciaAutomatica = 1` e `LojaOrigemEstoque = "05"`.

O vinculo permanece enquanto o registro estiver no historico de `Transferencia`. O campo persistente do item do pedido ainda nao foi identificado. Linhas repetidas com a mesma chave recebem o mesmo indicador, e essa chave nao distingue atendimento parcial entre elas. O codigo de barras vem do cadastro atual da variante; nao comprova o codigo historico do item vendido.

-- Executar no mesmo banco do ERP usado pela API. Apenas SELECTs.
-- Casos de /api/consulta-vendas-transferencias-automaticas:
-- 8008331-17: COLCHAO MOLA 174050 PANTANAL SUPER POKET, antes da impressao.
-- 8014276-11: CELULAR 169639 POCO C85, impresso e ainda nao efetivado.
-- 8008012-03: LONDRES 5 PORTAS, MALBEC, impresso e ja entregue.
-- O indicador esperado e 1 nos tres produtos, independentemente do estagio.
-- Nao filtrar por referencia presumida a partir da descricao do produto:
-- o primeiro resultado mostra as referencias realmente gravadas na venda.
SET NOCOUNT ON;

-- 1. Itens reais das vendas. Pode haver outros produtos no mesmo pedido;
-- confira referencia, cor e tamanho do produto indicado em cada exemplo.
SELECT
    v.pedido,
    v.loja,
    v.referencia,
    v.Cor,
    v.Numero,
    v.quant,
    v.data
FROM ConsultaVenda v
WHERE v.pedido IN ('8008331-17', '8014276-11', '8008012-03')
ORDER BY v.pedido, v.referencia, v.Cor, v.Numero;

-- 2. Transferencias sem filtros de impressao, efetivacao ou origem.
-- O endpoint exige Automatica = '1', origem 5, destino igual a loja,
-- NotaFiscal igual a pedido, referencia, descricao da cor e do tamanho.
-- statusimpressao e DataAuto sao exibidos apenas para conferir o estagio;
-- nao interferem no indicador da consulta de transferencias da venda.
SELECT
    t.NotaFiscal AS pedido,
    t.Origem,
    t.Destino,
    t.referencia,
    t.CodCor,
    c.Descricao AS Cor,
    t.CodNumero,
    n.Descricao AS Numero,
    t.Quant,
    t.Automatica,
    t.statusimpressao,
    t.DataAuto
FROM Transferencia t
LEFT JOIN Cor c ON c.Codigo = t.CodCor
LEFT JOIN Numeracao n ON n.Codigo = t.CodNumero
WHERE t.NotaFiscal IN ('8008331-17', '8014276-11', '8008012-03')
ORDER BY t.NotaFiscal, t.referencia, t.CodCor, t.CodNumero;

-- 3. Controle de transferencias: permite detectar se algum item existe
-- apenas no controle, sem registro correspondente em Transferencia.
-- O endpoint atual usa Transferencia. Um registro isolado no controle nao
-- comprova Automatica = '1', pois essa coluna nao foi identificada nele.
SELECT
    tc.NotaFiscal AS pedido,
    tc.Origem,
    tc.Destino,
    tc.Referencia,
    tc.Codprod,
    tc.CodCor,
    c.Descricao AS Cor,
    tc.CodNumero,
    n.Descricao AS Numero,
    tc.Quant,
    tc.Impresso,
    tc.Faturado,
    tc.Data
FROM TransferenciaControle tc
LEFT JOIN Cor c ON c.Codigo = tc.CodCor
LEFT JOIN Numeracao n ON n.Codigo = tc.CodNumero
WHERE tc.NotaFiscal IN ('8008331-17', '8014276-11', '8008012-03')
ORDER BY tc.NotaFiscal, tc.Referencia, tc.CodCor, tc.CodNumero;

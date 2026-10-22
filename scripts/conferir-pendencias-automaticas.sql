-- Executar no banco do ERP usado pela API. Apenas consultas.
-- Usa o criterio de pendencia da rota /api/transferencias-automaticas:
-- Automatica = '1', statusimpressao = '1' e DataAuto IS NULL.
-- Esse criterio nao comprova, sozinho, recebimento pela loja de destino.
SET NOCOUNT ON;

DECLARE @DataInicial datetime;
SET @DataInicial = '20260601';

-- 1. Controles desde 01/06/2026 com item automatico pendente correspondente.
-- O periodo e aplicado a tc.Data, como na lista atualizada entre lojas.
-- Se retornar linhas, sao automaticas pendentes pela regra acima que a
-- lista atual de lojas EXCLUI, pois o item ja existe em Transferencia.
-- O vinculo abaixo usa as mesmas oito chaves da lista atual de lojas.
SELECT tc.*
FROM TransferenciaControle tc
WHERE tc.Data >= @DataInicial
  AND EXISTS
  (
      SELECT 1
      FROM Transferencia t
      WHERE t.NotaFiscal = tc.NotaFiscal
        AND t.Origem = tc.Origem
        AND t.Destino = tc.Destino
        AND t.Codprod = tc.Codprod
        AND (t.CodCor = tc.CodCor OR (t.CodCor IS NULL AND tc.CodCor IS NULL))
        AND (t.CodNumero = tc.CodNumero OR (t.CodNumero IS NULL AND tc.CodNumero IS NULL))
        AND (t.IdEmpresa = tc.IdEmpresa OR (t.IdEmpresa IS NULL AND tc.IdEmpresa IS NULL))
        AND (t.IdFilial = tc.IdFilial OR (t.IdFilial IS NULL AND tc.IdFilial IS NULL))
        AND t.Automatica = '1'
        AND t.statusimpressao = '1'
        AND t.DataAuto IS NULL
  )
ORDER BY tc.Data DESC, tc.NotaFiscal, tc.Origem, tc.Destino, tc.Seq;

-- 2. Automaticas pendentes diretamente em Transferencia desde 01/06/2026,
-- incluindo itens sem controle correspondente. Aqui o periodo usa t.Data;
-- essa data pode diferir de tc.Data, portanto os resultados nao sao iguais.
-- Nao acrescenta vinculos com cadastros de produtos, cores ou numeracoes.
SELECT t.*
FROM Transferencia t
WHERE t.Data >= @DataInicial
  AND t.Automatica = '1'
  AND t.statusimpressao = '1'
  AND t.DataAuto IS NULL
ORDER BY t.Data DESC, t.NotaFiscal, t.Origem, t.Destino, t.Codprod;

-- Se o resultado 1 estiver vazio, isso nao classifica os controles isolados
-- como manuais: sem uma fonte de identificacao automatica nesses controles,
-- nao e possivel determinar seu tipo apenas pela ausencia em Transferencia.

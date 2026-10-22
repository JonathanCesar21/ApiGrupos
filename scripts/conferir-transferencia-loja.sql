-- Executar no mesmo banco do ERP usado pela API. Apenas consultas.
-- Nota confirmada pelo usuario como efetivada: 19102026134040.
-- Essa confirmacao e o resultado esperado, ainda nao validado na base.
SET NOCOUNT ON;

DECLARE @NotaFiscal varchar(50);
SET @NotaFiscal = '19102026134040';

-- 1. Registro de controle, incluindo os campos de situacao disponiveis.
SELECT tc.*
FROM TransferenciaControle tc
WHERE tc.NotaFiscal = @NotaFiscal
ORDER BY tc.Origem, tc.Destino, tc.Codprod, tc.Seq;

-- 2. Registro de transferencia, incluindo os campos de situacao disponiveis.
SELECT t.*
FROM Transferencia t
WHERE t.NotaFiscal = @NotaFiscal
ORDER BY t.Origem, t.Destino, t.Codprod;

-- 3. Itens que a regra atual da API retornaria como pendentes.
-- Para essa nota efetivada, o resultado esperado e nenhuma linha.
-- Se houver linhas, comparar as chaves dos resultados 1 e 2 antes de
-- alterar o vinculo ou presumir um campo de confirmacao pela loja destino.
SELECT tc.*
FROM TransferenciaControle tc
WHERE tc.NotaFiscal = @NotaFiscal
  AND tc.Data >= '20260601'
  AND NOT EXISTS
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
  )
ORDER BY tc.Data DESC, tc.NotaFiscal, tc.Origem, tc.Destino, tc.Seq;

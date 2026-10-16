-- Executar no mesmo banco do ERP usado pela API. Nao altera dados.
-- Valida o campo barras dos tres pedidos informados.
SET NOCOUNT ON;

-- 1. Codigo da rota /api/consulta-vendas-transferencias-automaticas.
-- Usa a mesma selecao por referencia + cor + tamanho e exige codigo unico.
-- Codigo unico no cadastro nao comprova o codigo historico do item vendido.
SELECT
    v.pedido,
    v.loja,
    v.referencia,
    v.Cor,
    v.Numero,
    produto.barras,
    produto.CodigosDistintos,
    CASE
        WHEN produto.CodigosDistintos = 1 THEN 'CODIGO_UNICO_NO_CADASTRO'
        WHEN produto.CodigosDistintos = 0 THEN 'SEM_CODIGO_NO_CADASTRO'
        ELSE 'VARIOS_CODIGOS_NA_VARIANTE'
    END AS SituacaoBarras
FROM ConsultaVenda v
CROSS APPLY
(
    SELECT
        CASE WHEN COUNT(DISTINCT pb.barras) = 1 THEN MIN(pb.barras) END AS barras,
        COUNT(DISTINCT pb.barras) AS CodigosDistintos
    FROM ProdutoBarras pb
    WHERE pb.referencia = v.referencia
      AND (pb.Cor = v.Cor OR (pb.Cor IS NULL AND v.Cor IS NULL))
      AND (pb.Numero = v.Numero OR (pb.Numero IS NULL AND v.Numero IS NULL))
      AND NULLIF(LTRIM(RTRIM(pb.barras)), '') IS NOT NULL
) produto
WHERE v.pedido IN ('8008331-17', '8014276-11', '8008012-03')
ORDER BY v.pedido, v.referencia, v.Cor, v.Numero;

-- 2. Todos os cadastros das referencias, incluindo outras cores/tamanhos,
-- para identificar divergencias de descricao ou mais de um codigo possivel.
SELECT
    pb.referencia,
    pb.Cor,
    pb.Numero,
    pb.barras,
    pb.DescProd
FROM ProdutoBarras pb
WHERE EXISTS
(
    SELECT 1
    FROM ConsultaVenda v
    WHERE v.pedido IN ('8008331-17', '8014276-11', '8008012-03')
      AND v.referencia = pb.referencia
)
ORDER BY pb.referencia, pb.Cor, pb.Numero, pb.barras;

-- 3. Se a propria ConsultaVenda ja expuser barras, consultar o valor direto
-- permite comparar a fonte da venda com o codigo obtido no cadastro atual.
-- O SQL dinamico evita erro de compilacao caso a coluna nao exista.
IF EXISTS
(
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'ConsultaVenda')
      AND name = N'barras'
)
BEGIN
    EXEC sys.sp_executesql N'
        SELECT pedido, loja, referencia, Cor, Numero, barras AS BarrasNaVenda
        FROM ConsultaVenda
        WHERE pedido IN (''8008331-17'', ''8014276-11'', ''8008012-03'')
        ORDER BY pedido, referencia, Cor, Numero;';
END
ELSE
BEGIN
    SELECT 'Coluna barras nao identificada em ConsultaVenda nos metadados visiveis.' AS Observacao;
END;

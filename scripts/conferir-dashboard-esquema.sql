-- SOMENTE LEITURA. Metadados e contagens, sem exportar dados de clientes.
-- Compativel com SQL Server 2005/2008; nao depende de sys.indexes.has_filter.
SELECT CAST(SERVERPROPERTY('ProductVersion') AS varchar(128)) AS versao;

SELECT s.name AS schemaNome, o.name AS objeto, o.type_desc,
       c.column_id, c.name AS coluna, t.name AS tipo,
       c.max_length, c.precision, c.scale, c.is_nullable
FROM sys.objects o
INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
INNER JOIN sys.columns c ON c.object_id = o.object_id
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE o.name IN ('ContaReceber','CReceber','CReceberCob','FormaPG','Metas','MetasVendedor',
                 'Vendedores','EntradaControle','ProdutoBarras','Transferencia','TransferenciaControle',
                 'Subgrupos','Grupos','ConsultaVenda','Produtos','centrada','Clientes','CobrancaTerceiros',
                 'Secao','Cor','Numeracao','Fornecedor','Categoria')
ORDER BY s.name, o.name, c.column_id;

SELECT s.name AS schemaNome, o.name AS objeto, i.name AS indice,
       i.is_primary_key, i.is_unique, ic.key_ordinal, c.name AS colunaChave
FROM sys.objects o
INNER JOIN sys.schemas s ON s.schema_id = o.schema_id
INNER JOIN sys.indexes i ON i.object_id = o.object_id
INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE o.name IN ('ContaReceber','CReceber','CReceberCob','FormaPG','Metas','MetasVendedor','Vendedores',
                 'EntradaControle','ProdutoBarras','Transferencia','TransferenciaControle','Subgrupos','Grupos','Produtos')
  AND i.is_unique = 1 AND ic.key_ordinal > 0
ORDER BY s.name, o.name, i.name, ic.key_ordinal;
-- Em SQL Server >= 2008 conferir adicionalmente has_filter/filter_definition
-- antes de tratar qualquer indice unico como chave de toda a origem.

SELECT 'Produtos.referencia' AS vinculo, COUNT_BIG(*) AS chavesComMultiplicidade
FROM (SELECT referencia FROM Produtos GROUP BY referencia HAVING COUNT_BIG(*) > 1) d
UNION ALL
SELECT 'Vendedores.codvend', COUNT_BIG(*)
FROM (SELECT codvend FROM Vendedores GROUP BY codvend HAVING COUNT_BIG(*) > 1) d
UNION ALL
SELECT 'FormaPG.codforma', COUNT_BIG(*)
FROM (SELECT codforma FROM FormaPG GROUP BY codforma HAVING COUNT_BIG(*) > 1) d
UNION ALL
SELECT 'ProdutoBarras.barras', COUNT_BIG(*)
FROM (SELECT barras FROM ProdutoBarras GROUP BY barras HAVING COUNT_BIG(*) > 1) d;

-- Comparar a cardinalidade do controle e do JOIN original em janela limitada.
DECLARE @Inicio datetime;
DECLARE @FimExclusivo datetime;
SET @Inicio = '20260610';
SET @FimExclusivo = '20260616';
SELECT
    (SELECT COUNT_BIG(*) FROM TransferenciaControle tc
     WHERE tc.Origem = 5 AND tc.Data >= @Inicio AND tc.Data < @FimExclusivo) AS linhasControle,
    (SELECT COUNT_BIG(*) FROM TransferenciaControle tc
     LEFT JOIN Transferencia t ON t.referencia = tc.referencia AND t.codcor = tc.codcor
          AND t.CodNumero = tc.CodNumero AND t.NotaFiscal = tc.NotaFiscal
          AND t.Origem = tc.Origem AND t.Destino = tc.Destino
     WHERE tc.Origem = 5 AND tc.Data >= @Inicio AND tc.Data < @FimExclusivo) AS linhasAposJoin;

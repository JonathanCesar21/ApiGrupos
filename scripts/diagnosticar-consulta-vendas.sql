-- Executar no banco do ERP usado pela API. Este script consulta apenas metadados;
-- nao altera tabelas/views nem retorna vendas, clientes ou credenciais.
-- Objetivo: identificar a origem persistente de "Sera entregue pelo deposito",
-- o codigo de barras vendido e a chave que identifica cada item do pedido.
SET NOCOUNT ON;

-- 1. Localizacao e definicao da consulta usada por /api/consulta-vendas.
SELECT
    SCHEMA_NAME(o.schema_id) AS Esquema,
    o.name AS Objeto,
    o.type_desc AS Tipo,
    OBJECT_DEFINITION(o.object_id) AS Definicao,
    HAS_PERMS_BY_NAME(
        QUOTENAME(SCHEMA_NAME(o.schema_id)) + '.' + QUOTENAME(o.name),
        'OBJECT', 'VIEW DEFINITION'
    ) AS PodeVerDefinicao
FROM sys.objects AS o
WHERE o.name = N'ConsultaVenda';

-- 2. Dependencias diretas da view. A definicao acima tambem permite seguir
-- eventuais views intermediarias e conferir joins, filtros e agregacoes.
SELECT
    OBJECT_SCHEMA_NAME(d.referencing_id) AS EsquemaConsulta,
    OBJECT_NAME(d.referencing_id) AS Consulta,
    d.referenced_server_name AS ServidorOrigem,
    d.referenced_database_name AS BancoOrigem,
    d.referenced_schema_name AS EsquemaOrigem,
    d.referenced_entity_name AS ObjetoOrigem,
    COL_NAME(d.referenced_id, d.referenced_minor_id) AS ColunaOrigem
FROM sys.sql_expression_dependencies AS d
WHERE OBJECT_NAME(d.referencing_id) = N'ConsultaVenda'
ORDER BY EsquemaConsulta, EsquemaOrigem, ObjetoOrigem, ColunaOrigem;

-- 3. Colunas da consulta, de suas dependencias locais e de objetos candidatos.
-- Nomes parecidos sao apenas pistas: nao confirmam o significado de uma flag.
SELECT
    SCHEMA_NAME(o.schema_id) AS Esquema,
    o.name AS Objeto,
    o.type_desc AS TipoObjeto,
    c.column_id AS Ordem,
    c.name AS Coluna,
    TYPE_NAME(c.user_type_id) AS TipoColuna,
    c.max_length AS TamanhoBytes,
    c.precision AS Precisao,
    c.scale AS Escala,
    c.is_nullable AS AceitaNulo,
    c.is_identity AS Identidade,
    dc.definition AS ValorPadrao,
    cc.definition AS ExpressaoCalculada,
    CONVERT(nvarchar(4000), ep.value) AS Descricao
FROM sys.objects AS o
INNER JOIN sys.columns AS c ON c.object_id = o.object_id
LEFT JOIN sys.default_constraints AS dc ON dc.object_id = c.default_object_id
LEFT JOIN sys.computed_columns AS cc
    ON cc.object_id = c.object_id AND cc.column_id = c.column_id
LEFT JOIN sys.extended_properties AS ep
    ON ep.class = 1
    AND ep.major_id = c.object_id
    AND ep.minor_id = c.column_id
    AND ep.name = N'MS_Description'
WHERE o.type IN ('U', 'V')
  AND (
      o.name = N'ConsultaVenda'
      OR o.object_id IN (
          SELECT d.referenced_id
          FROM sys.sql_expression_dependencies AS d
          WHERE OBJECT_NAME(d.referencing_id) = N'ConsultaVenda'
      )
      OR o.name LIKE N'%pedido%'
      OR o.name LIKE N'%venda%'
      OR c.name LIKE N'%deposit%'
      OR c.name LIKE N'%entreg%'
      OR c.name LIKE N'%automatic%'
      OR c.name LIKE N'%origem%'
  )
ORDER BY Esquema, Objeto, c.column_id;

-- 4. Chaves/indices unicos para conferir se o vinculo preserva uma linha por
-- item, inclusive quando o pedido repete referencia, cor e tamanho.
SELECT
    SCHEMA_NAME(o.schema_id) AS Esquema,
    o.name AS Objeto,
    i.name AS Indice,
    i.is_primary_key AS ChavePrimaria,
    i.filter_definition AS Filtro,
    ic.key_ordinal AS OrdemChave,
    c.name AS Coluna
FROM sys.objects AS o
INNER JOIN sys.indexes AS i ON i.object_id = o.object_id
INNER JOIN sys.index_columns AS ic
    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
INNER JOIN sys.columns AS c
    ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.is_unique = 1
  AND ic.key_ordinal > 0
  AND (
      o.name = N'ConsultaVenda'
      OR o.object_id IN (
          SELECT d.referenced_id
          FROM sys.sql_expression_dependencies AS d
          WHERE OBJECT_NAME(d.referencing_id) = N'ConsultaVenda'
      )
      OR o.name LIKE N'%pedido%'
      OR o.name LIKE N'%venda%'
  )
ORDER BY Esquema, Objeto, Indice, OrdemChave;

-- Se Definicao vier NULL, confira Tipo e PodeVerDefinicao: tabelas nao tem
-- definicao de view; permissoes restritas ou modulos criptografados tambem
-- podem impedir a leitura. Nesse caso, solicite a definicao ao responsavel ERP.
-- Confirmar no ERP qual coluna representa a opcao do item e se ela permanece
-- gravada depois da expedicao. Nao deduzir a flag por transferencias pendentes.

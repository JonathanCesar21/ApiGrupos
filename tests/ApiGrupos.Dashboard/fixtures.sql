-- Dados SINTETICOS, usados somente na conexao de teste e somente em #temporarias.
SET NOCOUNT ON;
CREATE TABLE #FormaPG (codforma int, Forma varchar(100), Tipo varchar(20));
INSERT INTO #FormaPG
SELECT 1,' DINHEIRO','A'
UNION ALL SELECT 2,'MAQ CIELO  2-10X','A'
UNION ALL SELECT 3,'MAQ STONE PIX','A'
UNION ALL SELECT 4,'CARTÃO AO BARULHO','P';
CREATE TABLE #ContaReceber (FormaPagto int, Operador varchar(30), Valor decimal(18,2), vlpago decimal(18,2), Baixa datetime, Empresa int, EmpresaRec int, Vencimento datetime, Pedido varchar(50));
INSERT INTO #ContaReceber
SELECT 1,'TESTE',50,10,'20260610',3,3,'20260701','P1'
UNION ALL SELECT 2,'TESTE',50,20,'20260611',3,5,'20260701',NULL
UNION ALL SELECT 3,'TESTE',50,30,'20260615 23:59:59.997',3,5,'20260701','REC AG.'
UNION ALL SELECT 4,'TESTE',50,40,'20260612',3,5,'20260610','P2'
UNION ALL SELECT 4,'TESTE',900,900,'20210101',3,5,'20260610','ANTIGO';
CREATE TABLE #CReceber (CodCli int, Pedido varchar(50), Parcela int, NParcelas int, Empresa int, Valor decimal(18,2), Vencimento datetime, DtPedido datetime, Pago bit, Baixa datetime, FormaPagto int);
INSERT INTO #CReceber
SELECT 1,'ANTIGO',1,1,3,10,'20190101','20180101',1,'20190101',1
UNION ALL SELECT 2,'PAGO-FALSE',1,1,3,20,'20200101','20190101',0,NULL,1
UNION ALL SELECT 3,'PAGO-NULL-BAIXA',1,1,3,30,'20260610','20260601',NULL,'20260610',1
UNION ALL SELECT 4,'AMBOS-NULL',1,1,3,40,'20260610','20210101',NULL,NULL,1;
CREATE TABLE #Metas (Loja int, Ano int, Mes int, Vendas decimal(18,2), Avista decimal(18,2), Receber decimal(18,2), Terceiros decimal(18,2), ValorRM decimal(18,2));
INSERT INTO #Metas
SELECT 3,2026,6,100,10,20,30,40
UNION ALL SELECT 3,2026,6,200,20,40,60,80
UNION ALL SELECT 3,2021,6,999,0,0,0,0;
CREATE TABLE #Vendedores (codVend varchar(10), vendedor varchar(50), Loja int, Inativo bit);
INSERT INTO #Vendedores
SELECT '10','HOMONIMO',9,1
UNION ALL SELECT '20','HOMONIMO',3,0;
CREATE TABLE #MetasVendedor (Loja int, CodVend varchar(10), Ano int, Mes int, Vendas decimal(18,2));
INSERT INTO #MetasVendedor
SELECT 3,'10',2026,6,100;
CREATE TABLE #EntradaControle (Loja int, Quant decimal(18,2), Barras varchar(30), NotaFiscal varchar(50), Data datetime);
INSERT INTO #EntradaControle
SELECT 3,1,'0001','ANTIGA','20200101'
UNION ALL SELECT 3,-3,'0001','ATUAL','20260610';
CREATE TABLE #ProdutoBarras (Barras varchar(30), Valor decimal(18,2), Grupo varchar(50), SubGrupo varchar(50), Fornecedor varchar(50), Referencia varchar(50), Secao varchar(50), Numero varchar(20), Cor varchar(20));
INSERT INTO #ProdutoBarras
SELECT '0001',10,'G','SG','F','OLD','S','M','AZUL';
CREATE TABLE #Grupos (codgrupo int, nomegrupo varchar(50));
INSERT INTO #Grupos
SELECT 1,'INFORMATICA'
UNION ALL SELECT 2,'MOVEIS';
CREATE TABLE #Subgrupos (codsubgrupo int, nomesubgrupo varchar(50), CodGrupo int, ClFiscal varchar(20), grupo int);
INSERT INTO #Subgrupos
SELECT 1,'IGUAL',1,'0001',2
UNION ALL SELECT 2,'IGUAL',2,'0002',2
UNION ALL SELECT 3,'ORFAO',999,NULL,2;
CREATE TABLE #Secao (Codigo int, Descricao varchar(50));
INSERT INTO #Secao
SELECT 1,'SECAO';
CREATE TABLE #Cor (Codigo int, Descricao varchar(50));
INSERT INTO #Cor
SELECT 1,'AZUL';
CREATE TABLE #Numeracao (Codigo int, Descricao varchar(50));
INSERT INTO #Numeracao
SELECT 1,'M';
CREATE TABLE #Fornecedor (cod_for int, nome_for varchar(50));
INSERT INTO #Fornecedor
SELECT 1,'FORNECEDOR';
CREATE TABLE #Categoria (codigo int, nome varchar(50));
INSERT INTO #Categoria
SELECT 101,'FEMININO';
CREATE TABLE #Produtos (codigo int, referencia varchar(50), Custo decimal(18,2), Secao int, Grupo int, subgrupo int, fornecedor int, Categoria int, Venda decimal(18,2), Colecao int);
INSERT INTO #Produtos
SELECT 1,'OLD',4,1,1,1,1,101,20,1
UNION ALL SELECT 2,'OLD',999,1,1,1,1,101,999,1
UNION ALL SELECT 3,'TR',4,1,1,1,1,101,20,1;
CREATE TABLE #Transferencia (Referencia varchar(50), codcor int, CodNumero int, CodFor int, Quant decimal(18,2), NotaFiscal varchar(50), Destino int, Origem int, statusimpressao bit, Data datetime, DataAuto datetime, UsuarioAuto varchar(20), UsuarioConfirma varchar(20), Automatica bit);
INSERT INTO #Transferencia
SELECT 'TR',1,1,1,1,'A',3,5,0,'20260110','20260111',NULL,NULL,1
UNION ALL SELECT 'TR',1,1,1,1,'B',3,5,1,'20240110',NULL,'AUTORIZADOR',NULL,1
UNION ALL SELECT 'TR',1,1,1,1,'C',3,5,NULL,'20260110',NULL,NULL,NULL,0
UNION ALL SELECT 'TR',1,1,1,1,'C',3,5,NULL,'20260110',NULL,NULL,'CONFIRMADOR',0;
CREATE TABLE #TransferenciaControle (Referencia varchar(50), codcor int, CodNumero int, CodFor int, Quant decimal(18,2), NotaFiscal varchar(50), Destino int, Origem int, Data datetime);
INSERT INTO #TransferenciaControle
SELECT 'TR',1,1,1,1,'C',3,5,'20260110'
UNION ALL SELECT 'TR',1,1,1,1,'D',3,5,'20260110'
UNION ALL SELECT 'TR',1,1,1,1,'E',3,4,'20260110';
CREATE TABLE #Clientes (codigo int, nome varchar(50), Cadastro datetime);
INSERT INTO #Clientes
SELECT 1,'SINTETICO 1','20180101'
UNION ALL SELECT 2,'SINTETICO 2','20260601'
UNION ALL SELECT 3,'SINTETICO 3','20180101'
UNION ALL SELECT 1376401,'EXCLUIDO','20260601';
CREATE TABLE #ConsultaVenda (codcli int, data datetime, loja int, total decimal(18,2), pedido varchar(50), vendedor varchar(50), quant decimal(18,2), ValorTotal decimal(18,2), ValorCusto decimal(18,2), referencia varchar(50), Numero varchar(20), Grupo varchar(50), SubGrupo varchar(50), Fornecedor varchar(50));
INSERT INTO #ConsultaVenda
SELECT 1,'20190101',3,100,'OLD1','10',1,100,10,'OLD','M','G','SG','F'
UNION ALL SELECT 3,'20190101',3,100,'OLD3','10',1,100,10,'OLD','M','G','SG','F'
UNION ALL SELECT 1,'20260611',3,80,'PEX','10',2,80,10,'OLD','M','G','SG','F'
UNION ALL SELECT 2,'20260610 09:00',3,50,'P2','10',1,10,5,'OLD','M','G','SG','F'
UNION ALL SELECT 2,'20260610 09:00',3,50,'P2','10',1,0,0,'OLD','M','G','SG','F'
UNION ALL SELECT 2,'20260610 09:00',3,-5,'P3','10',-1,0,0,'OLD','M','G','SG','F'
UNION ALL SELECT 1376401,'20260610',3,0,'EXCL','10',0,0,0,'OLD','M','G','SG','F'
UNION ALL SELECT 3,'20260612',3,12,'ZERO1','10',1,12,20,'ZERO','M','G','SG','F'
UNION ALL SELECT 3,'20260612',3,-10,'ZERO2','10',-1,-10,10,'ZERO','M','G','SG','F';
CREATE TABLE #centrada (Data datetime, NotaFiscal varchar(50), Referencia varchar(50), Loja int, Quant decimal(18,2), Valor decimal(18,2), CodNumero int, codfor int, CodProd int, barras varchar(30));
INSERT INTO #centrada
SELECT '20260610','N1','OLD',3,999,4,1,1,1,'B1'
UNION ALL SELECT '20260710','N1','OLD',3,999,4,1,1,1,'B1'
UNION ALL SELECT '20260611','N2','OLD',3,1,4,1,1,1,'B1'
UNION ALL SELECT '20260611','N2','OLD',3,1,4,1,1,1,'B2'
UNION ALL SELECT '20260611','N3','OLD',3,1,4,1,1,1,'B1';
CREATE TABLE #CobrancaTerceiros (Codigo int, Nome varchar(50));
INSERT INTO #CobrancaTerceiros
SELECT 1,'COBRANCA SINTETICA';
CREATE TABLE #CReceberCob (Codcli int, Vencimento datetime, Valor decimal(18,2), Pedido varchar(50), DtPedido datetime, Parcela int, NParcelas int, CodCobranca int, Empresa int, Status int, Recebimento datetime);
INSERT INTO #CReceberCob
SELECT 1,'20260610',80,'T1','20260101',1,1,1,3,0,'20260611'
UNION ALL SELECT 1,'20260610',999,'ORFAO','20260101',1,1,999,3,0,NULL
UNION ALL SELECT 1,'20260610',999,'STATUS1','20260101',1,1,1,3,1,NULL;

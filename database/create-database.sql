SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET NOEXEC OFF;
GO

IF DB_ID() > 4
BEGIN
    RAISERROR(N'Este script cria o próprio banco e não prepara o banco em que está conectado; nada foi alterado. Rode-o conectado ao master (sem -d) e, para outro nome, troque o nome nas linhas do topo do script.', 16, 1);
    SET NOEXEC ON;
END
ELSE IF DB_ID(N'Sophia') IS NULL CREATE DATABASE [Sophia] COLLATE Latin1_General_CI_AS;
GO

USE [Sophia];
GO

DECLARE @esperado TABLE (Item NVARCHAR(200) NOT NULL PRIMARY KEY);
INSERT INTO @esperado (Item)
SELECT CONCAT(tabela.Nome, N'.', item.value)
FROM (VALUES
    (N'Clientes', N'Id,Cpf Latin1_General_CI_AS,Nome Latin1_General_CI_AI,DataNascimento,Email Latin1_General_CI_AS,Telefone Latin1_General_CI_AS,PK_Clientes,UQ_Clientes_Cpf,CK_Clientes_Cpf_SomenteDigitos,CK_Clientes_Telefone_SomenteDigitos,IX_Clientes_Nome'),
    (N'Produtos', N'Id,Nome Latin1_General_CI_AI,ValorUnitario,PK_Produtos,UQ_Produtos_Nome,CK_Produtos_ValorUnitario_Positivo,CK_Produtos_ValorUnitarioMaximo'),
    (N'Vendas', N'Id,Data,ClienteId,ChaveDeRegistro,PK_Vendas,FK_Vendas_Clientes,IX_Vendas_ClienteId,IX_Vendas_Data,UQ_Vendas_ChaveDeRegistro'),
    (N'VendaItens', N'Id,VendaId,ProdutoId,NomeDoProduto Latin1_General_CI_AI,Quantidade,ValorUnitario,PK_VendaItens,FK_VendaItens_Vendas,FK_VendaItens_Produtos,UQ_VendaItens_Venda_Produto,CK_VendaItens_Quantidade_Positiva,CK_VendaItens_QuantidadeMaxima,CK_VendaItens_ValorUnitario_Positivo,CK_VendaItens_ValorUnitarioMaximo,IX_VendaItens_ProdutoId')
) AS tabela (Nome, Itens)
CROSS APPLY STRING_SPLIT(tabela.Itens, N',') AS item;

DECLARE @existente TABLE (Item NVARCHAR(200) NOT NULL PRIMARY KEY);
INSERT INTO @existente (Item)
SELECT CONCAT(t.name, N'.', c.name, N' ' + c.collation_name) COLLATE DATABASE_DEFAULT
FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
WHERE t.schema_id = SCHEMA_ID(N'dbo')
UNION
SELECT CONCAT(t.name, N'.', o.name) COLLATE DATABASE_DEFAULT
FROM sys.objects o JOIN sys.tables t ON t.object_id = o.parent_object_id
WHERE t.schema_id = SCHEMA_ID(N'dbo')
UNION
SELECT CONCAT(t.name, N'.', i.name) COLLATE DATABASE_DEFAULT
FROM sys.indexes i JOIN sys.tables t ON t.object_id = i.object_id
WHERE t.schema_id = SCHEMA_ID(N'dbo') AND i.name IS NOT NULL;

DECLARE @agrupamento SYSNAME = CONVERT(SYSNAME, DATABASEPROPERTYEX(DB_NAME(), N'Collation'));
DECLARE @faltam NVARCHAR(MAX) = (
    SELECT STRING_AGG(Item, N', ') WITHIN GROUP (ORDER BY Item)
    FROM (SELECT Item FROM @esperado EXCEPT SELECT Item FROM @existente) AS falta);
DECLARE @recusa NVARCHAR(2000);

IF DB_ID() <= 4
    SET @recusa = N'O banco do sistema não pôde ser criado nem aberto, e o script parou sem alterar nada. Confira a mensagem anterior e se o login pode criar bancos.';
ELSE IF @agrupamento <> N'Latin1_General_CI_AS'
    SET @recusa = CONCAT(N'O banco ', QUOTENAME(DB_NAME()), N' usa o agrupamento ', @agrupamento,
        N', e as tabelas precisam de Latin1_General_CI_AS; nada foi alterado. ',
        N'Use outro nome, trocado nas linhas do topo do script, ou guarde o que precisar e apague o banco antes de rodar de novo.');
ELSE IF EXISTS (
    SELECT 1 FROM sys.tables
    WHERE is_ms_shipped = 0
      AND NOT (schema_id = SCHEMA_ID(N'dbo') AND name IN (N'Clientes', N'Produtos', N'Vendas', N'VendaItens')))
    SET @recusa = CONCAT(N'O banco ', QUOTENAME(DB_NAME()), N' já tem tabelas de outro sistema; nada foi alterado. ',
        N'Use um banco vazio: troque o nome do banco nas linhas do topo do script.');
ELSE IF EXISTS (SELECT 1 FROM @existente) AND @faltam IS NOT NULL
    SET @recusa = LEFT(CONCAT(N'O banco ', QUOTENAME(DB_NAME()),
        N' tem as tabelas de uma versão anterior do script; nada foi alterado. ',
        N'Guarde os dados que precisar, apague o banco (no Docker, docker compose down -v) e rode o script de novo, ou use outro nome, trocado nas linhas do topo. Falta: ', @faltam, N'.'), 2000);

IF @recusa IS NOT NULL
BEGIN
    RAISERROR(N'%s', 16, 1, @recusa);
    SET NOEXEC ON;
END
GO

SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
BEGIN
    BEGIN TRANSACTION;

    CREATE TABLE dbo.Clientes
    (
        Id             INT           IDENTITY(1, 1) NOT NULL,
        Cpf            CHAR(11)                     NOT NULL,
        Nome           NVARCHAR(150) COLLATE Latin1_General_CI_AI NOT NULL,
        DataNascimento DATE                         NULL,
        Email          NVARCHAR(254)                NULL,
        Telefone       VARCHAR(11)                  NULL,
        CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Clientes_Cpf UNIQUE (Cpf),
        CONSTRAINT CK_Clientes_Cpf_SomenteDigitos CHECK (Cpf NOT LIKE '%[^0-9]%'),
        CONSTRAINT CK_Clientes_Telefone_SomenteDigitos CHECK (Telefone NOT LIKE '%[^0-9]%' AND LEN(Telefone) IN (10, 11))
    );
    CREATE NONCLUSTERED INDEX IX_Clientes_Nome ON dbo.Clientes (Nome);

    CREATE TABLE dbo.Produtos
    (
        Id            INT            IDENTITY(1, 1) NOT NULL,
        Nome          NVARCHAR(150)  COLLATE Latin1_General_CI_AI NOT NULL,
        ValorUnitario DECIMAL(18, 2)                NOT NULL,
        CONSTRAINT PK_Produtos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Produtos_Nome UNIQUE (Nome),
        CONSTRAINT CK_Produtos_ValorUnitario_Positivo CHECK (ValorUnitario > 0),
        CONSTRAINT CK_Produtos_ValorUnitarioMaximo CHECK (ValorUnitario <= 999999.99)
    );

    CREATE TABLE dbo.Vendas
    (
        Id              INT              IDENTITY(1, 1) NOT NULL,
        Data            DATE                            NOT NULL,
        ClienteId       INT                             NOT NULL,
        ChaveDeRegistro UNIQUEIDENTIFIER                NULL,
        CONSTRAINT PK_Vendas PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Vendas_Clientes FOREIGN KEY (ClienteId) REFERENCES dbo.Clientes (Id) ON DELETE NO ACTION
    );
    CREATE NONCLUSTERED INDEX IX_Vendas_ClienteId ON dbo.Vendas (ClienteId);
    CREATE NONCLUSTERED INDEX IX_Vendas_Data ON dbo.Vendas (Data DESC);
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Vendas_ChaveDeRegistro ON dbo.Vendas (ChaveDeRegistro)
        WHERE ChaveDeRegistro IS NOT NULL;

    CREATE TABLE dbo.VendaItens
    (
        Id            INT            IDENTITY(1, 1) NOT NULL,
        VendaId       INT                           NOT NULL,
        ProdutoId     INT                           NOT NULL,
        NomeDoProduto NVARCHAR(150) COLLATE Latin1_General_CI_AI NOT NULL,
        Quantidade    INT                           NOT NULL,
        ValorUnitario DECIMAL(18, 2)                NOT NULL,
        CONSTRAINT PK_VendaItens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_VendaItens_Vendas FOREIGN KEY (VendaId) REFERENCES dbo.Vendas (Id) ON DELETE CASCADE,
        CONSTRAINT FK_VendaItens_Produtos FOREIGN KEY (ProdutoId) REFERENCES dbo.Produtos (Id) ON DELETE NO ACTION,
        CONSTRAINT UQ_VendaItens_Venda_Produto UNIQUE (VendaId, ProdutoId),
        CONSTRAINT CK_VendaItens_Quantidade_Positiva CHECK (Quantidade > 0),
        CONSTRAINT CK_VendaItens_QuantidadeMaxima CHECK (Quantidade <= 999999),
        CONSTRAINT CK_VendaItens_ValorUnitario_Positivo CHECK (ValorUnitario > 0),
        CONSTRAINT CK_VendaItens_ValorUnitarioMaximo CHECK (ValorUnitario <= 999999.99)
    );
    CREATE NONCLUSTERED INDEX IX_VendaItens_ProdutoId ON dbo.VendaItens (ProdutoId);

    COMMIT TRANSACTION;
END
GO

SET NOEXEC OFF;
GO

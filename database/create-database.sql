SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_ID() <= 4 AND DB_ID(N'Sophia') IS NULL
BEGIN
    CREATE DATABASE [Sophia] COLLATE Latin1_General_CI_AS;
END
GO

DECLARE @banco SYSNAME = IIF(DB_ID() <= 4, N'Sophia', DB_NAME());
DECLARE @noBanco NVARCHAR(300) = QUOTENAME(@banco) + N'.sys.sp_executesql';

PRINT CONCAT(N'Criando ou atualizando o banco ', @banco, N'.');

EXEC @noBanco N'
IF CONVERT(SYSNAME, DATABASEPROPERTYEX(DB_NAME(), N''Collation'')) <> N''Latin1_General_CI_AS''
BEGIN
    IF EXISTS (SELECT 1 FROM sys.tables WHERE is_ms_shipped = 0)
        THROW 50001, N''O banco escolhido já tem tabelas e não usa o agrupamento Latin1_General_CI_AS; crie um banco vazio com COLLATE Latin1_General_CI_AS.'', 1;

    ALTER DATABASE CURRENT COLLATE Latin1_General_CI_AS;
END';

EXEC @noBanco N'
IF OBJECT_ID(N''dbo.Clientes'', N''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Clientes
    (
        Id             INT            IDENTITY(1, 1) NOT NULL,
        Cpf            CHAR(11)                      NOT NULL,
        Nome           NVARCHAR(150)                 NOT NULL,
        DataNascimento DATE                          NULL,
        Email          NVARCHAR(254)                 NULL,
        Telefone       VARCHAR(11)                   NULL,
        CONSTRAINT PK_Clientes PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Clientes_Cpf UNIQUE (Cpf),
        CONSTRAINT CK_Clientes_Cpf_SomenteDigitos CHECK (Cpf NOT LIKE ''%[^0-9]%''),
        CONSTRAINT CK_Clientes_Telefone_SomenteDigitos CHECK (Telefone NOT LIKE ''%[^0-9]%'' AND LEN(Telefone) IN (10, 11))
    );

    CREATE NONCLUSTERED INDEX IX_Clientes_Nome ON dbo.Clientes (Nome);
END';

EXEC @noBanco N'
IF OBJECT_ID(N''dbo.Produtos'', N''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos
    (
        Id            INT            IDENTITY(1, 1) NOT NULL,
        Nome          NVARCHAR(150)                 NOT NULL,
        ValorUnitario DECIMAL(18, 2)                NOT NULL,
        CONSTRAINT PK_Produtos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Produtos_Nome UNIQUE (Nome),
        CONSTRAINT CK_Produtos_ValorUnitario_Positivo CHECK (ValorUnitario > 0)
    );
END';

EXEC @noBanco N'
IF OBJECT_ID(N''dbo.Vendas'', N''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Vendas
    (
        Id              INT              IDENTITY(1, 1) NOT NULL,
        Data            DATE                            NOT NULL,
        ClienteId       INT                             NOT NULL,
        ChaveDeRegistro UNIQUEIDENTIFIER                NULL,
        CONSTRAINT PK_Vendas PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Vendas_Clientes FOREIGN KEY (ClienteId)
            REFERENCES dbo.Clientes (Id) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX IX_Vendas_ClienteId ON dbo.Vendas (ClienteId);
    CREATE NONCLUSTERED INDEX IX_Vendas_Data ON dbo.Vendas (Data DESC);
END';

EXEC @noBanco N'
IF COL_LENGTH(N''dbo.Vendas'', N''ChaveDeRegistro'') IS NULL
BEGIN
    ALTER TABLE dbo.Vendas ADD ChaveDeRegistro UNIQUEIDENTIFIER NULL;
END';

EXEC @noBanco N'
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N''dbo.Vendas'') AND name = N''UQ_Vendas_ChaveDeRegistro'')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Vendas_ChaveDeRegistro
        ON dbo.Vendas (ChaveDeRegistro)
        WHERE ChaveDeRegistro IS NOT NULL;
END';

EXEC @noBanco N'
IF OBJECT_ID(N''dbo.VendaItens'', N''U'') IS NULL
BEGIN
    CREATE TABLE dbo.VendaItens
    (
        Id            INT            IDENTITY(1, 1) NOT NULL,
        VendaId       INT                           NOT NULL,
        ProdutoId     INT                           NOT NULL,
        Quantidade    INT                           NOT NULL,
        ValorUnitario DECIMAL(18, 2)                NOT NULL,
        CONSTRAINT PK_VendaItens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_VendaItens_Vendas FOREIGN KEY (VendaId)
            REFERENCES dbo.Vendas (Id) ON DELETE CASCADE,
        CONSTRAINT FK_VendaItens_Produtos FOREIGN KEY (ProdutoId)
            REFERENCES dbo.Produtos (Id) ON DELETE NO ACTION,
        CONSTRAINT UQ_VendaItens_Venda_Produto UNIQUE (VendaId, ProdutoId),
        CONSTRAINT CK_VendaItens_Quantidade_Positiva CHECK (Quantidade > 0),
        CONSTRAINT CK_VendaItens_ValorUnitario_Positivo CHECK (ValorUnitario > 0)
    );

    CREATE NONCLUSTERED INDEX IX_VendaItens_ProdutoId ON dbo.VendaItens (ProdutoId);
END';
GO

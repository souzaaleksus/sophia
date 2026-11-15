SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ARITHABORT ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @deSistema BIT = IIF(DB_ID() <= 4 OR DB_NAME() IN (N'master', N'tempdb', N'model', N'msdb'), 1, 0);

IF @deSistema = 1 AND DB_ID(N'Sophia') IS NULL
BEGIN
    CREATE DATABASE [Sophia] COLLATE Latin1_General_CI_AS;
END

DECLARE @banco SYSNAME = IIF(@deSistema = 1, N'Sophia', DB_NAME());
DECLARE @noBanco NVARCHAR(300) = QUOTENAME(@banco) + N'.sys.sp_executesql';

PRINT CONCAT(N'Criando ou atualizando o banco ', @banco, N'.');

EXEC @noBanco N'
IF EXISTS (
    SELECT 1 FROM sys.tables
    WHERE is_ms_shipped = 0
      AND NOT (SCHEMA_NAME(schema_id) = N''dbo'' AND name IN (N''Clientes'', N''Produtos'', N''Vendas'', N''VendaItens'')))
    THROW 50002, N''O banco escolhido já tem outras tabelas; use um banco vazio, ou conecte-se ao master para o script criar o banco Sophia.'', 1;';

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
        Nome           NVARCHAR(150)  COLLATE Latin1_General_CI_AI NOT NULL,
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
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N''dbo.Clientes'') AND name = N''Nome''
      AND collation_name <> N''Latin1_General_CI_AI'')
BEGIN
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N''dbo.Clientes'') AND name = N''IX_Clientes_Nome'')
        DROP INDEX IX_Clientes_Nome ON dbo.Clientes;

    ALTER TABLE dbo.Clientes ALTER COLUMN Nome NVARCHAR(150) COLLATE Latin1_General_CI_AI NOT NULL;
    CREATE NONCLUSTERED INDEX IX_Clientes_Nome ON dbo.Clientes (Nome);

    COMMIT TRANSACTION;
END';

EXEC @noBanco N'
IF OBJECT_ID(N''dbo.Produtos'', N''U'') IS NULL
BEGIN
    CREATE TABLE dbo.Produtos
    (
        Id            INT            IDENTITY(1, 1) NOT NULL,
        Nome          NVARCHAR(150)  COLLATE Latin1_General_CI_AI NOT NULL,
        ValorUnitario DECIMAL(18, 2)                NOT NULL,
        CONSTRAINT PK_Produtos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Produtos_Nome UNIQUE (Nome),
        CONSTRAINT CK_Produtos_ValorUnitario_Positivo CHECK (ValorUnitario > 0)
    );
END';

EXEC @noBanco N'
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N''dbo.Produtos'') AND name = N''Nome''
      AND collation_name <> N''Latin1_General_CI_AI'')
BEGIN
    DECLARE @colidentes NVARCHAR(MAX) = (
        SELECT STRING_AGG(CONVERT(NVARCHAR(MAX), p.Nome), N'' | '') WITHIN GROUP (ORDER BY p.Nome, p.Id)
        FROM dbo.Produtos p
        WHERE EXISTS (
            SELECT 1 FROM dbo.Produtos o
            WHERE o.Id <> p.Id AND o.Nome COLLATE Latin1_General_CI_AI = p.Nome COLLATE Latin1_General_CI_AI));

    IF @colidentes IS NOT NULL
    BEGIN
        DECLARE @mensagem NVARCHAR(2048) = LEFT(CONCAT(
            N''A coluna Produtos.Nome não pôde passar a ignorar acentos, porque estes nomes ficariam repetidos sem eles: '',
            @colidentes,
            N''. Renomeie esses produtos e rode o script de novo.''), 2048);
        THROW 50003, @mensagem, 1;
    END

    BEGIN TRANSACTION;

    IF OBJECT_ID(N''dbo.UQ_Produtos_Nome'', N''UQ'') IS NOT NULL
        ALTER TABLE dbo.Produtos DROP CONSTRAINT UQ_Produtos_Nome;

    ALTER TABLE dbo.Produtos ALTER COLUMN Nome NVARCHAR(150) COLLATE Latin1_General_CI_AI NOT NULL;
    ALTER TABLE dbo.Produtos ADD CONSTRAINT UQ_Produtos_Nome UNIQUE (Nome);

    COMMIT TRANSACTION;
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
        CONSTRAINT CK_VendaItens_QuantidadeMaxima CHECK (Quantidade <= 999999),
        CONSTRAINT CK_VendaItens_ValorUnitario_Positivo CHECK (ValorUnitario > 0)
    );

    CREATE NONCLUSTERED INDEX IX_VendaItens_ProdutoId ON dbo.VendaItens (ProdutoId);
END';

EXEC @noBanco N'
IF OBJECT_ID(N''dbo.CK_VendaItens_QuantidadeMaxima'', N''C'') IS NULL
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.VendaItens WHERE Quantidade > 999999)
        THROW 50004, N''Há itens de venda com quantidade acima de 999.999, e a restrição CK_VendaItens_QuantidadeMaxima não pôde ser criada. Corrija esses itens e rode o script de novo.'', 1;

    ALTER TABLE dbo.VendaItens WITH CHECK
        ADD CONSTRAINT CK_VendaItens_QuantidadeMaxima CHECK (Quantidade <= 999999);
END';
GO

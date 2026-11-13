IF DB_ID(N'Sophia') IS NULL
BEGIN
    CREATE DATABASE [Sophia];
END
GO

USE [Sophia];
GO

IF OBJECT_ID(N'dbo.Clientes', N'U') IS NULL
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
        CONSTRAINT CK_Clientes_Cpf_SomenteDigitos CHECK (Cpf NOT LIKE '%[^0-9]%'),
        CONSTRAINT CK_Clientes_Telefone_SomenteDigitos CHECK (Telefone NOT LIKE '%[^0-9]%' AND LEN(Telefone) IN (10, 11))
    );

    CREATE NONCLUSTERED INDEX IX_Clientes_Nome ON dbo.Clientes (Nome);
END
GO

IF OBJECT_ID(N'dbo.Produtos', N'U') IS NULL
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
END
GO

IF OBJECT_ID(N'dbo.Vendas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vendas
    (
        Id        INT  IDENTITY(1, 1) NOT NULL,
        Data      DATE                NOT NULL,
        ClienteId INT                 NOT NULL,
        CONSTRAINT PK_Vendas PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Vendas_Clientes FOREIGN KEY (ClienteId)
            REFERENCES dbo.Clientes (Id) ON DELETE NO ACTION
    );

    CREATE NONCLUSTERED INDEX IX_Vendas_ClienteId ON dbo.Vendas (ClienteId);
    CREATE NONCLUSTERED INDEX IX_Vendas_Data ON dbo.Vendas (Data DESC);
END
GO

IF OBJECT_ID(N'dbo.VendaItens', N'U') IS NULL
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
END
GO

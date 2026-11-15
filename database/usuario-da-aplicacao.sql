SET NOCOUNT ON;
SET NOEXEC OFF;
GO

IF DB_ID() <= 4
BEGIN
    RAISERROR(N'Este script cria o usuário da aplicação no banco do Sophia e não roda num banco de sistema; nada foi alterado. Rode-o com -d e o nome do banco (por exemplo, -d Sophia).', 16, 1);
    SET NOEXEC ON;
END
GO

SET XACT_ABORT ON;

IF SUSER_ID(N'sophia_app') IS NULL
    CREATE LOGIN sophia_app WITH PASSWORD = N'$(SenhaDaAplicacao)', CHECK_POLICY = ON;
ELSE
    ALTER LOGIN sophia_app WITH PASSWORD = N'$(SenhaDaAplicacao)';

IF USER_ID(N'sophia_app') IS NULL
    CREATE USER sophia_app FOR LOGIN sophia_app;

ALTER ROLE db_datareader ADD MEMBER sophia_app;
ALTER ROLE db_datawriter ADD MEMBER sophia_app;
GO

SET NOEXEC OFF;
GO

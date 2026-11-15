SET NOCOUNT ON;
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

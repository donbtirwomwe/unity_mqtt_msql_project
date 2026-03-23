DECLARE @AppDbUser sysname = N'__APP_DB_USER__';
DECLARE @AppDbPassword nvarchar(256) = N'__APP_DB_PASSWORD__';

IF NOT EXISTS (SELECT 1 FROM master.sys.sql_logins WHERE name = @AppDbUser)
BEGIN
    DECLARE @CreateLoginSql nvarchar(max) = N'CREATE LOGIN ' + QUOTENAME(@AppDbUser) + N' WITH PASSWORD = ' + QUOTENAME(@AppDbPassword, '''') + N', CHECK_POLICY = ON;';
    EXEC(@CreateLoginSql);
END;

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @AppDbUser)
BEGIN
    DECLARE @CreateUserSql nvarchar(max) = N'CREATE USER ' + QUOTENAME(@AppDbUser) + N' FOR LOGIN ' + QUOTENAME(@AppDbUser) + N';';
    EXEC(@CreateUserSql);
END;

DECLARE @AlterUserSql nvarchar(max) = N'ALTER USER ' + QUOTENAME(@AppDbUser) + N' WITH DEFAULT_SCHEMA = [dbo];';
EXEC(@AlterUserSql);

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_GetAssetList
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, Description
    FROM dbo.ASSETS
    ORDER BY ID;
END');

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_GetAssetDetails
    @id NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1)
        Name,
        Description,
        Status,
        MessagingRole,
        MessagingRoleCode
    FROM dbo.ASSETS
    WHERE ID = @id;
END');

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_GetAssetDataPoints
    @assetId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, Description
    FROM dbo.DATAPOINTS
    WHERE ASSET_ID = @assetId
    ORDER BY ID;
END');

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_GetDataPointDetails
    @id NVARCHAR(64),
    @assetId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) Name, Description, Status
    FROM dbo.DATAPOINTS
    WHERE ID = @id AND ASSET_ID = @assetId;
END');

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_GetDataPointFiles
    @assetId NVARCHAR(64),
    @dpid NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, Description, Type, Link
    FROM dbo.DATAFILES
    WHERE ASSET_ID = @assetId AND DATAPOINT_ID = @dpid
    ORDER BY ID;
END');

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_GetDataPointChannels
    @assetId NVARCHAR(64),
    @dpid NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, Description, Target
    FROM dbo.DATACHANNELS
    WHERE ASSET_ID = @assetId AND DATAPOINT_ID = @dpid
    ORDER BY ID;
END');

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_ResolveAssetId
    @input NVARCHAR(1024)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) ID
    FROM dbo.ASSETS
    WHERE ID = @input OR Name = @input OR Description LIKE ''%'' + @input + ''%''
    ORDER BY CASE WHEN ID = @input THEN 0 WHEN Name = @input THEN 1 ELSE 2 END;
END');

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_GetAssetCount
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) AS AssetCount
    FROM dbo.ASSETS;
END');

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_GetAssetPreview
    @limit INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@limit) ID, Name
    FROM dbo.ASSETS
    ORDER BY ID;
END');

EXEC(N'
CREATE OR ALTER PROCEDURE dbo.usp_GetAssetTopics
    @id NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.Name, d.Target
    FROM dbo.ASSETS a
    JOIN dbo.DATACHANNELS d ON a.ID = d.ASSET_ID
    WHERE a.ID = @id
    ORDER BY d.ID;
END');

DECLARE @GrantExecSql nvarchar(max) = N'GRANT EXECUTE TO ' + QUOTENAME(@AppDbUser) + N';';
EXEC(@GrantExecSql);

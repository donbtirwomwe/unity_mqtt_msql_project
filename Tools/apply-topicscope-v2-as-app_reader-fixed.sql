-- ==============================================================================
-- TOPIC INDIRECTION + ASSET SCOPING v2 (APP_READER SELF-EXECUTE)
-- ==============================================================================
-- Execute this as app_reader (after SA grants schema rights)
-- Creates topic UID layer and asset access control

SET NOCOUNT ON;
GO

-- ==============================================================================
-- CREATE TOPICMAPPINGS TABLE (if not exists)
-- ==============================================================================
IF OBJECT_ID(N'dbo.TOPICMAPPINGS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TOPICMAPPINGS (
        ID NVARCHAR(64) NOT NULL PRIMARY KEY,
        CHANNEL_ID NVARCHAR(64) NOT NULL UNIQUE,
        ASSET_ID NVARCHAR(64) NOT NULL,
        TopicUID NVARCHAR(256) NOT NULL,
        RealTopicPath NVARCHAR(1024) NOT NULL,
        CreatedAt DATETIME DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_TOPICMAPPINGS_AssetId ON dbo.TOPICMAPPINGS(ASSET_ID);
    CREATE INDEX IX_TOPICMAPPINGS_TopicUID ON dbo.TOPICMAPPINGS(TopicUID);
    PRINT 'Created table: TOPICMAPPINGS';
END;
GO

-- ==============================================================================
-- CREATE ASSETACCESS TABLE (if not exists)
-- ==============================================================================
IF OBJECT_ID(N'dbo.ASSETACCESS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ASSETACCESS (
        ID NVARCHAR(64) NOT NULL PRIMARY KEY,
        USER_ID NVARCHAR(64) NOT NULL,
        ASSET_ID NVARCHAR(64) NOT NULL,
        CreatedAt DATETIME DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_ASSETACCESS_UserId ON dbo.ASSETACCESS(USER_ID);
    CREATE INDEX IX_ASSETACCESS_AssetId ON dbo.ASSETACCESS(ASSET_ID);
    PRINT 'Created table: ASSETACCESS';
END;
GO

-- ==============================================================================
-- POPULATE TOPICMAPPINGS FROM EXISTING DATACHANNELS (one-time init)
-- ==============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.TOPICMAPPINGS)
BEGIN
    INSERT INTO dbo.TOPICMAPPINGS (ID, CHANNEL_ID, ASSET_ID, TopicUID, RealTopicPath)
    SELECT 
        NEWID(),
        dc.ID,
        dc.ASSET_ID,
        'uid_' + dc.ASSET_ID + '_' + dc.DATAPOINT_ID + '_' + dc.ID,
        dc.Target
    FROM dbo.DATACHANNELS dc;
    PRINT 'Populated TOPICMAPPINGS with ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' entries.';
END;
GO

-- ==============================================================================
-- POPULATE ASSETACCESS: default app_reader access to all assets
-- ==============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.ASSETACCESS WHERE USER_ID = 'app_reader')
BEGIN
    INSERT INTO dbo.ASSETACCESS (ID, USER_ID, ASSET_ID)
    SELECT 
        NEWID(),
        'app_reader',
        a.ID
    FROM dbo.ASSETS a;
    PRINT 'Populated ASSETACCESS for app_reader on all ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' assets.';
END;
GO

-- ==============================================================================
-- PROCEDURES: Return TopicUID instead of raw topic paths
-- ==============================================================================

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetList
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, Description
    FROM dbo.ASSETS
    WHERE IS_MEMBER('db_owner') = 1
        OR EXISTS (
            SELECT 1
            FROM dbo.ASSETACCESS aa
            WHERE aa.ASSET_ID = dbo.ASSETS.ID
                AND aa.USER_ID = SYSTEM_USER
        )
    ORDER BY ID;
END;
GO

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
    WHERE ID = @id
        AND (
            IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = dbo.ASSETS.ID
                    AND aa.USER_ID = SYSTEM_USER
            )
        );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetDataPoints
    @assetId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, Description
    FROM dbo.DATAPOINTS
    WHERE ASSET_ID = @assetId
        AND (
            IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = @assetId
                    AND aa.USER_ID = SYSTEM_USER
            )
        )
    ORDER BY ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetDataPointDetails
    @id NVARCHAR(64),
    @assetId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) Name, Description, Status
    FROM dbo.DATAPOINTS
    WHERE ID = @id
        AND ASSET_ID = @assetId
        AND (
            IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = @assetId
                    AND aa.USER_ID = SYSTEM_USER
            )
        );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetDataPointFiles
    @assetId NVARCHAR(64),
    @dpid NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, Description, Type, Link
    FROM dbo.DATAFILES
    WHERE ASSET_ID = @assetId
        AND DATAPOINT_ID = @dpid
        AND (
            IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = @assetId
                    AND aa.USER_ID = SYSTEM_USER
            )
        )
    ORDER BY ID;
END;
GO

-- **KEY CHANGE**: Return TopicUID instead of raw Target
CREATE OR ALTER PROCEDURE dbo.usp_GetDataPointChannels
    @assetId NVARCHAR(64),
    @dpid NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        dc.ID, 
        dc.Name, 
        dc.Description, 
        COALESCE(tm.TopicUID, dc.Target) AS Target,
        COALESCE(tm.RealTopicPath, dc.Target) AS RealTopicPath
    FROM dbo.DATACHANNELS dc
    LEFT JOIN dbo.TOPICMAPPINGS tm ON dc.ID = tm.CHANNEL_ID
    WHERE dc.ASSET_ID = @assetId
        AND dc.DATAPOINT_ID = @dpid
        AND (
            IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = @assetId
                    AND aa.USER_ID = SYSTEM_USER
            )
        )
    ORDER BY dc.ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ResolveAssetId
    @input NVARCHAR(1024)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) ID
    FROM dbo.ASSETS
    WHERE (ID = @input OR Name = @input OR Description LIKE '%' + @input + '%')
        AND (
            IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = dbo.ASSETS.ID
                    AND aa.USER_ID = SYSTEM_USER
            )
        )
    ORDER BY CASE WHEN ID = @input THEN 0 WHEN Name = @input THEN 1 ELSE 2 END;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetCount
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) AS AssetCount
    FROM dbo.ASSETS
    WHERE IS_MEMBER('db_owner') = 1
        OR EXISTS (
            SELECT 1
            FROM dbo.ASSETACCESS aa
            WHERE aa.ASSET_ID = dbo.ASSETS.ID
                AND aa.USER_ID = SYSTEM_USER
        );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetPreview
    @limit INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@limit) ID, Name
    FROM dbo.ASSETS
    WHERE IS_MEMBER('db_owner') = 1
        OR EXISTS (
            SELECT 1
            FROM dbo.ASSETACCESS aa
            WHERE aa.ASSET_ID = dbo.ASSETS.ID
                AND aa.USER_ID = SYSTEM_USER
        )
    ORDER BY ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetTopics
    @id NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        a.Name,
        COALESCE(tm.TopicUID, d.Target) AS Target,
        COALESCE(tm.RealTopicPath, d.Target) AS RealTopicPath
    FROM dbo.ASSETS a
    JOIN dbo.DATACHANNELS d ON a.ID = d.ASSET_ID
    LEFT JOIN dbo.TOPICMAPPINGS tm ON d.ID = tm.CHANNEL_ID
    WHERE a.ID = @id
        AND (
            IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = a.ID
                    AND aa.USER_ID = SYSTEM_USER
            )
        )
    ORDER BY d.ID;
END;
GO

-- ==============================================================================
-- NEW PROCEDURES: Topic resolution and asset scoping
-- ==============================================================================

CREATE OR ALTER PROCEDURE dbo.usp_GetTopicMapping
    @topicUID NVARCHAR(256),
    @assetId NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) RealTopicPath
    FROM dbo.TOPICMAPPINGS
    WHERE TopicUID = @topicUID
        AND (@assetId IS NULL OR ASSET_ID = @assetId)
        AND (
            IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = dbo.TOPICMAPPINGS.ASSET_ID
                    AND aa.USER_ID = SYSTEM_USER
            )
        );
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAccessibleTopics
    @assetId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        tm.TopicUID,
        tm.CHANNEL_ID,
        dc.Name,
        dc.Description
    FROM dbo.TOPICMAPPINGS tm
    JOIN dbo.DATACHANNELS dc ON tm.CHANNEL_ID = dc.ID
    WHERE tm.ASSET_ID = @assetId
        AND (
            IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = @assetId
                    AND aa.USER_ID = SYSTEM_USER
            )
        )
    ORDER BY dc.ID;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetMqttAclEntries
    @userId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    WITH ScopedAssets AS (
        SELECT a.ID, a.MessagingRole, a.MessagingRoleCode
        FROM dbo.ASSETS a
        WHERE IS_MEMBER('db_owner') = 1
            OR EXISTS (
                SELECT 1
                FROM dbo.ASSETACCESS aa
                WHERE aa.ASSET_ID = a.ID
                    AND aa.USER_ID = @userId
            )
    )
    SELECT DISTINCT
        CASE
            WHEN sa.MessagingRoleCode = 1 OR LOWER(ISNULL(sa.MessagingRole, '')) = 'publisher' THEN 'write'
            WHEN sa.MessagingRoleCode = 0 OR LOWER(ISNULL(sa.MessagingRole, '')) = 'subscriber' THEN 'read'
            ELSE 'readwrite'
        END AS AccessMode,
        COALESCE(tm.RealTopicPath, dc.Target) AS TopicPath
    FROM ScopedAssets sa
    JOIN dbo.DATACHANNELS dc ON dc.ASSET_ID = sa.ID
    LEFT JOIN dbo.TOPICMAPPINGS tm ON tm.CHANNEL_ID = dc.ID
    WHERE COALESCE(tm.RealTopicPath, dc.Target) IS NOT NULL
    ORDER BY TopicPath;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAccessibleAssets
AS
BEGIN
    SET NOCOUNT ON;
    SELECT DISTINCT
        a.ID,
        a.Name,
        a.Description
    FROM dbo.ASSETS a
    INNER JOIN dbo.ASSETACCESS aa ON a.ID = aa.ASSET_ID
    WHERE aa.USER_ID = SYSTEM_USER
    ORDER BY a.ID;
END;
GO

PRINT '';
PRINT '========================================';
PRINT 'Topic indirection v2 schema applied successfully.';
PRINT 'All channels now accessible via TopicUID instead of raw topic paths.';
PRINT '========================================';

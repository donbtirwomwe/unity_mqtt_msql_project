-- ==============================================================================
-- TOPIC INDIRECTION + ASSET SCOPING v2 (APP_READER SELF-EXECUTE)
-- ==============================================================================
-- Execute this as app_reader (after SA grants schema rights)
-- Creates topic UID layer and asset access control

SET NOCOUNT ON;

-- ==============================================================================
-- CREATE TOPICMAPPINGS TABLE (if not exists)
-- Maps channel IDs to opaque topic UIDs; indirection layer for MQTT security
-- ==============================================================================
IF OBJECT_ID(N'dbo.TOPICMAPPINGS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TOPICMAPPINGS (
        ID NVARCHAR(64) NOT NULL PRIMARY KEY,
        CHANNEL_ID NVARCHAR(64) NOT NULL UNIQUE,
        ASSET_ID NVARCHAR(64) NOT NULL,
        TopicUID NVARCHAR(256) NOT NULL,           -- Opaque identifier (e.g., uid_a101_c101_e101)
        RealTopicPath NVARCHAR(1024) NOT NULL,    -- Actual MQTT topic (stored but scoped)
        CreatedAt DATETIME DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_TOPICMAPPINGS_AssetId ON dbo.TOPICMAPPINGS(ASSET_ID);
    CREATE INDEX IX_TOPICMAPPINGS_TopicUID ON dbo.TOPICMAPPINGS(TopicUID);
    PRINT 'Created table: TOPICMAPPINGS';
END;

-- ==============================================================================
-- CREATE ASSETACCESS TABLE (if not exists)
-- Links app_reader to allowed assets (foundation for fine-grained scoping)
-- ==============================================================================
IF OBJECT_ID(N'dbo.ASSETACCESS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ASSETACCESS (
        ID NVARCHAR(64) NOT NULL PRIMARY KEY,
        USER_ID NVARCHAR(64) NOT NULL,            -- e.g., 'app_reader'
        ASSET_ID NVARCHAR(64) NOT NULL,           -- Which asset this user can access
        CreatedAt DATETIME DEFAULT GETUTCDATE()
    );
    CREATE INDEX IX_ASSETACCESS_UserId ON dbo.ASSETACCESS(USER_ID);
    CREATE INDEX IX_ASSETACCESS_AssetId ON dbo.ASSETACCESS(ASSET_ID);
    PRINT 'Created table: ASSETACCESS';
END;

-- ==============================================================================
-- POPULATE TOPICMAPPINGS FROM EXISTING DATACHANNELS (one-time init)
-- Generates TopicUID as: uid_assetid_dpid_chid (opaque but structured)
-- ==============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.TOPICMAPPINGS)
BEGIN
    INSERT INTO dbo.TOPICMAPPINGS (ID, CHANNEL_ID, ASSET_ID, TopicUID, RealTopicPath)
    SELECT 
        NEWID(),
        dc.ID,
        dc.ASSET_ID,
        'uid_' + dc.ASSET_ID + '_' + dc.DATAPOINT_ID + '_' + dc.ID,  -- TopicUID pattern
        dc.Target
    FROM dbo.DATACHANNELS dc;
    PRINT 'Populated TOPICMAPPINGS with ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' entries.';
END;

-- ==============================================================================
-- POPULATE ASSETACCESS: default app_reader access to all assets
-- Can be refined later to restrict per asset (e.g., app_reader_a101 only sees a101)
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

-- ==============================================================================
-- UPDATE PROCEDURES: Return TopicUID instead of raw topic paths
-- ==============================================================================

GO

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetList
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, Description
    FROM dbo.ASSETS
    ORDER BY ID;
END;
PRINT 'Created/updated: usp_GetAssetList';

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
END;
PRINT 'Created/updated: usp_GetAssetDetails';

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetDataPoints
    @assetId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Name, Description
    FROM dbo.DATAPOINTS
    WHERE ASSET_ID = @assetId
    ORDER BY ID;
END;
PRINT 'Created/updated: usp_GetAssetDataPoints';

CREATE OR ALTER PROCEDURE dbo.usp_GetDataPointDetails
    @id NVARCHAR(64),
    @assetId NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) Name, Description, Status
    FROM dbo.DATAPOINTS
    WHERE ID = @id AND ASSET_ID = @assetId;
END;
PRINT 'Created/updated: usp_GetDataPointDetails';

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
END;
PRINT 'Created/updated: usp_GetDataPointFiles';

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
        COALESCE(tm.TopicUID, dc.Target) AS Target
    FROM dbo.DATACHANNELS dc
    LEFT JOIN dbo.TOPICMAPPINGS tm ON dc.ID = tm.CHANNEL_ID
    WHERE dc.ASSET_ID = @assetId AND dc.DATAPOINT_ID = @dpid
    ORDER BY dc.ID;
END;
PRINT 'Created/updated: usp_GetDataPointChannels (now returns TopicUID)';

CREATE OR ALTER PROCEDURE dbo.usp_ResolveAssetId
    @input NVARCHAR(1024)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) ID
    FROM dbo.ASSETS
    WHERE ID = @input OR Name = @input OR Description LIKE '%' + @input + '%'
    ORDER BY CASE WHEN ID = @input THEN 0 WHEN Name = @input THEN 1 ELSE 2 END;
END;
PRINT 'Created/updated: usp_ResolveAssetId';

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetCount
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COUNT(1) AS AssetCount
    FROM dbo.ASSETS;
END;
PRINT 'Created/updated: usp_GetAssetCount';

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetPreview
    @limit INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (@limit) ID, Name
    FROM dbo.ASSETS
    ORDER BY ID;
END;
PRINT 'Created/updated: usp_GetAssetPreview';

CREATE OR ALTER PROCEDURE dbo.usp_GetAssetTopics
    @id NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        a.Name,
        COALESCE(tm.TopicUID, d.Target) AS Target
    FROM dbo.ASSETS a
    JOIN dbo.DATACHANNELS d ON a.ID = d.ASSET_ID
    LEFT JOIN dbo.TOPICMAPPINGS tm ON d.ID = tm.CHANNEL_ID
    WHERE a.ID = @id
    ORDER BY d.ID;
END;
PRINT 'Created/updated: usp_GetAssetTopics (now returns TopicUID)';

-- ==============================================================================
-- NEW PROCEDURES: Topic resolution and asset scoping
-- ==============================================================================

-- Resolve TopicUID back to real MQTT topic (scoped enforcement possible here)
CREATE OR ALTER PROCEDURE dbo.usp_GetTopicMapping
    @topicUID NVARCHAR(256),
    @assetId NVARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP (1) 
        TopicUID, 
        RealTopicPath, 
        ASSET_ID
    FROM dbo.TOPICMAPPINGS
    WHERE TopicUID = @topicUID
        AND (@assetId IS NULL OR ASSET_ID = @assetId);
END;
PRINT 'Created: usp_GetTopicMapping';

-- List all topic UIDs accessible by current user for a given asset
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
    ORDER BY dc.ID;
END;
PRINT 'Created: usp_GetAccessibleTopics';

-- List assets accessible to current user (via ASSETACCESS table)
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
PRINT 'Created: usp_GetAccessibleAssets';

PRINT '';
PRINT '========================================';
PRINT 'Topic indirection v2 schema applied successfully.';
PRINT 'All channels now accessible via TopicUID instead of raw topic paths.';
PRINT '========================================';

-- ==============================================================================
-- GRANT TABLE CREATION & MANAGEMENT RIGHTS TO app_reader
-- Run this as SA first, then app_reader can self-manage schema
-- ==============================================================================

DECLARE @AppDbUser sysname = N'__APP_DB_USER__';

-- Grant schema permissions so app_reader can create tables, procedures, etc.
DECLARE @GrantSchemaSql nvarchar(max) = N'
GRANT CREATE TABLE TO ' + QUOTENAME(@AppDbUser) + N';
GRANT ALTER ON SCHEMA::dbo TO ' + QUOTENAME(@AppDbUser) + N';
GRANT CREATE PROCEDURE TO ' + QUOTENAME(@AppDbUser) + N';
GRANT ALTER ON SCHEMA::dbo TO ' + QUOTENAME(@AppDbUser) + N';
GRANT VIEW DEFINITION ON SCHEMA::dbo TO ' + QUOTENAME(@AppDbUser) + N';
GRANT EXECUTE ON SCHEMA::dbo TO ' + QUOTENAME(@AppDbUser) + N';
';

EXEC(@GrantSchemaSql);

Print 'Permissions granted to ' + @AppDbUser + '. User can now create/modify tables and procedures.';

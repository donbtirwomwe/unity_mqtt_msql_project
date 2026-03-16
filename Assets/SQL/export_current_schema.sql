SET NOCOUNT ON;

PRINT '=== SCHEMA EXPORT: ASSETS, DATAPOINTS, DATACHANNELS, DATAFILES ===';
PRINT 'Database: ' + DB_NAME();
PRINT 'Timestamp (UTC): ' + CONVERT(varchar(33), GETUTCDATE(), 126) + 'Z';

DECLARE @TargetTables TABLE (TableName sysname);
INSERT INTO @TargetTables (TableName)
VALUES ('ASSETS'), ('DATAPOINTS'), ('DATACHANNELS'), ('DATAFILES');

PRINT '--- 1) Table Presence ---';
SELECT
    t.TableName,
    CASE WHEN o.object_id IS NULL THEN 'MISSING' ELSE 'FOUND' END AS Status,
    s.name AS SchemaName,
    o.name AS ObjectName
FROM @TargetTables t
LEFT JOIN sys.objects o
    ON o.name = t.TableName
   AND o.type = 'U'
LEFT JOIN sys.schemas s
    ON s.schema_id = o.schema_id
ORDER BY t.TableName;

PRINT '--- 2) Columns ---';
SELECT
    sch.name AS TableSchema,
    tbl.name AS TableName,
    col.column_id AS ColumnOrder,
    col.name AS ColumnName,
    typ.name AS DataType,
    col.max_length AS MaxLengthBytes,
    col.precision AS NumericPrecision,
    col.scale AS NumericScale,
    col.is_nullable AS IsNullable,
    col.is_identity AS IsIdentity,
    dc.definition AS DefaultDefinition
FROM sys.tables tbl
JOIN sys.schemas sch
    ON sch.schema_id = tbl.schema_id
JOIN sys.columns col
    ON col.object_id = tbl.object_id
JOIN sys.types typ
    ON typ.user_type_id = col.user_type_id
LEFT JOIN sys.default_constraints dc
    ON dc.parent_object_id = col.object_id
   AND dc.parent_column_id = col.column_id
WHERE tbl.name IN (SELECT TableName FROM @TargetTables)
ORDER BY tbl.name, col.column_id;

PRINT '--- 3) Primary/Unique Keys ---';
SELECT
    sch.name AS TableSchema,
    tbl.name AS TableName,
    kc.name AS ConstraintName,
    kc.type_desc AS ConstraintType,
    col.name AS ColumnName,
    ic.key_ordinal AS KeyOrdinal
FROM sys.key_constraints kc
JOIN sys.tables tbl
    ON tbl.object_id = kc.parent_object_id
JOIN sys.schemas sch
    ON sch.schema_id = tbl.schema_id
JOIN sys.index_columns ic
    ON ic.object_id = kc.parent_object_id
   AND ic.index_id = kc.unique_index_id
JOIN sys.columns col
    ON col.object_id = ic.object_id
   AND col.column_id = ic.column_id
WHERE tbl.name IN (SELECT TableName FROM @TargetTables)
ORDER BY tbl.name, kc.name, ic.key_ordinal;

PRINT '--- 4) Foreign Keys ---';
SELECT
    fk.name AS FKName,
    schChild.name AS ChildSchema,
    tChild.name AS ChildTable,
    cChild.name AS ChildColumn,
    schParent.name AS ParentSchema,
    tParent.name AS ParentTable,
    cParent.name AS ParentColumn,
    fk.delete_referential_action_desc AS OnDelete,
    fk.update_referential_action_desc AS OnUpdate
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc
    ON fkc.constraint_object_id = fk.object_id
JOIN sys.tables tChild
    ON tChild.object_id = fk.parent_object_id
JOIN sys.schemas schChild
    ON schChild.schema_id = tChild.schema_id
JOIN sys.columns cChild
    ON cChild.object_id = fkc.parent_object_id
   AND cChild.column_id = fkc.parent_column_id
JOIN sys.tables tParent
    ON tParent.object_id = fk.referenced_object_id
JOIN sys.schemas schParent
    ON schParent.schema_id = tParent.schema_id
JOIN sys.columns cParent
    ON cParent.object_id = fkc.referenced_object_id
   AND cParent.column_id = fkc.referenced_column_id
WHERE tChild.name IN (SELECT TableName FROM @TargetTables)
   OR tParent.name IN (SELECT TableName FROM @TargetTables)
ORDER BY tChild.name, fk.name, fkc.constraint_column_id;

PRINT '--- 5) Indexes (non-heap) ---';
SELECT
    sch.name AS TableSchema,
    tbl.name AS TableName,
    idx.name AS IndexName,
    idx.type_desc AS IndexType,
    idx.is_unique AS IsUnique,
    idx.is_primary_key AS IsPrimaryKey,
    col.name AS ColumnName,
    ic.key_ordinal AS KeyOrdinal,
    ic.is_included_column AS IsIncluded
FROM sys.indexes idx
JOIN sys.tables tbl
    ON tbl.object_id = idx.object_id
JOIN sys.schemas sch
    ON sch.schema_id = tbl.schema_id
JOIN sys.index_columns ic
    ON ic.object_id = idx.object_id
   AND ic.index_id = idx.index_id
JOIN sys.columns col
    ON col.object_id = ic.object_id
   AND col.column_id = ic.column_id
WHERE tbl.name IN (SELECT TableName FROM @TargetTables)
  AND idx.type > 0
ORDER BY tbl.name, idx.name, ic.key_ordinal, ic.index_column_id;

PRINT '=== END SCHEMA EXPORT ===';

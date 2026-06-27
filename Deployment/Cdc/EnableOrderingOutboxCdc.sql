USE ECommerceOrderingDb;
GO

-- CDC NOTE:
-- Run EF migrations before this script so dbo.OutboxMessages exists.
-- SQL Server Agent must be running for CDC capture and cleanup jobs.
-- This setup should be re-runnable because deployment automation may execute it more than once.

IF NOT EXISTS (
    SELECT 1
    FROM sys.databases
    WHERE name = DB_NAME()
      AND is_cdc_enabled = 1
)
BEGIN
    EXEC sys.sp_cdc_enable_db;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = N'OutboxMessages'
      AND schema_id = SCHEMA_ID(N'dbo')
      AND is_tracked_by_cdc = 1
)
BEGIN
    EXEC sys.sp_cdc_enable_table
        @source_schema = N'dbo',
        @source_name = N'OutboxMessages',
        @role_name = NULL,
        @supports_net_changes = 0;
END
GO

SELECT
    name,
    is_cdc_enabled
FROM sys.databases
WHERE name = N'ECommerceOrderingDb';

SELECT
    name,
    is_tracked_by_cdc
FROM sys.tables
WHERE name = N'OutboxMessages';

EXEC sys.sp_cdc_help_jobs;
GO

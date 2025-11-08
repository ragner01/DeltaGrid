-- Initial schema for DeltaGrid
-- Version: 1.001
-- Environment: All

IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'deltagrid')
BEGIN
    EXEC('CREATE SCHEMA deltagrid');
END
GO

-- Outbox table for transactional messaging
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Outbox' AND schema_id = SCHEMA_ID('deltagrid'))
BEGIN
    CREATE TABLE [deltagrid].[Outbox] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [AggregateId] NVARCHAR(255) NOT NULL,
        [EventType] NVARCHAR(255) NOT NULL,
        [Payload] NVARCHAR(MAX) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ProcessedAt] DATETIME2 NULL
    );
    
    CREATE INDEX IX_Outbox_ProcessedAt ON [deltagrid].[Outbox] ([ProcessedAt]) WHERE [ProcessedAt] IS NULL;
END
GO

-- Permits table (hash-chained archive)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PermitArchive' AND schema_id = SCHEMA_ID('deltagrid'))
BEGIN
    CREATE TABLE [deltagrid].[PermitArchive] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [PermitId] UNIQUEIDENTIFIER NOT NULL,
        [PayloadHash] NVARCHAR(64) NOT NULL,
        [PrevHash] NVARCHAR(64) NULL,
        [ChainHash] NVARCHAR(64) NOT NULL,
        [Version] INT NOT NULL,
        [ArchivedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
    
    CREATE INDEX IX_PermitArchive_PermitId ON [deltagrid].[PermitArchive] ([PermitId]);
    CREATE INDEX IX_PermitArchive_ChainHash ON [deltagrid].[PermitArchive] ([ChainHash]);
END
GO

-- Admin audit log
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdminAuditLog' AND schema_id = SCHEMA_ID('deltagrid'))
BEGIN
    CREATE TABLE [deltagrid].[AdminAuditLog] (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [UserId] NVARCHAR(255) NOT NULL,
        [UserName] NVARCHAR(255) NOT NULL,
        [Action] NVARCHAR(255) NOT NULL,
        [ResourceType] NVARCHAR(255) NOT NULL,
        [ResourceId] NVARCHAR(255) NULL,
        [Timestamp] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [IpAddress] NVARCHAR(45) NULL,
        [UserAgent] NVARCHAR(500) NULL,
        [Success] BIT NOT NULL DEFAULT 1,
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [Metadata] NVARCHAR(MAX) NULL  -- JSON metadata
    );
    
    CREATE INDEX IX_AdminAuditLog_UserId ON [deltagrid].[AdminAuditLog] ([UserId]);
    CREATE INDEX IX_AdminAuditLog_Action ON [deltagrid].[AdminAuditLog] ([Action]);
    CREATE INDEX IX_AdminAuditLog_Timestamp ON [deltagrid].[AdminAuditLog] ([Timestamp]);
END
GO



-- Reporting schema additions
-- Version: 1.002
-- Environment: All

-- Report templates
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReportTemplates' AND schema_id = SCHEMA_ID('deltagrid'))
BEGIN
    CREATE TABLE [deltagrid].[ReportTemplates] (
        [Id] NVARCHAR(255) NOT NULL PRIMARY KEY,
        [Name] NVARCHAR(255) NOT NULL,
        [Type] INT NOT NULL,  -- ReportType enum
        [Version] NVARCHAR(50) NOT NULL,
        [TemplateContent] NVARCHAR(MAX) NOT NULL,
        [Parameters] NVARCHAR(MAX) NULL,  -- JSON array
        [Region] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [CreatedBy] NVARCHAR(255) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [Metadata] NVARCHAR(MAX) NULL  -- JSON metadata
    );
    
    CREATE INDEX IX_ReportTemplates_Type ON [deltagrid].[ReportTemplates] ([Type]);
    CREATE INDEX IX_ReportTemplates_Region ON [deltagrid].[ReportTemplates] ([Region]);
END
GO

-- Generated reports
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'GeneratedReports' AND schema_id = SCHEMA_ID('deltagrid'))
BEGIN
    CREATE TABLE [deltagrid].[GeneratedReports] (
        [Id] NVARCHAR(255) NOT NULL PRIMARY KEY,
        [TemplateId] NVARCHAR(255) NOT NULL,
        [TemplateVersion] NVARCHAR(50) NOT NULL,
        [Type] INT NOT NULL,
        [TenantId] NVARCHAR(255) NOT NULL,
        [Content] VARBINARY(MAX) NOT NULL,
        [Format] INT NOT NULL,
        [ContentType] NVARCHAR(100) NOT NULL,
        [FileName] NVARCHAR(255) NOT NULL,
        [GeneratedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [GeneratedBy] NVARCHAR(255) NULL,
        [ReportDate] DATETIME2 NULL,
        [SiteId] NVARCHAR(255) NULL,
        [AssetId] NVARCHAR(255) NULL,
        [Parameters] NVARCHAR(MAX) NULL,  -- JSON
        [Status] INT NOT NULL DEFAULT 0,
        [Watermark] NVARCHAR(100) NULL,
        [SignatureHash] NVARCHAR(64) NULL
    );
    
    CREATE INDEX IX_GeneratedReports_TemplateId ON [deltagrid].[GeneratedReports] ([TemplateId]);
    CREATE INDEX IX_GeneratedReports_TenantId ON [deltagrid].[GeneratedReports] ([TenantId]);
    CREATE INDEX IX_GeneratedReports_GeneratedAt ON [deltagrid].[GeneratedReports] ([GeneratedAt]);
END
GO

-- Report signatures
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ReportSignatures' AND schema_id = SCHEMA_ID('deltagrid'))
BEGIN
    CREATE TABLE [deltagrid].[ReportSignatures] (
        [ReportId] NVARCHAR(255) NOT NULL,
        [SignerId] NVARCHAR(255) NOT NULL,
        [SignerName] NVARCHAR(255) NOT NULL,
        [Role] NVARCHAR(100) NOT NULL,
        [SignedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [SignatureHash] NVARCHAR(64) NOT NULL,
        [Comment] NVARCHAR(MAX) NULL,
        PRIMARY KEY ([ReportId], [SignerId])
    );
    
    CREATE INDEX IX_ReportSignatures_SignerId ON [deltagrid].[ReportSignatures] ([SignerId]);
END
GO



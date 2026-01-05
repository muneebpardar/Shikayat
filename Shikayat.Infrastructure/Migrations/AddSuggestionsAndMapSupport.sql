-- Migration: AddSuggestionsAndMapSupport
-- Run this script on your SQL Server database

USE [ShikayatDb];
GO

-- Add SuggestionId column to ComplaintLogs
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ComplaintLogs]') AND name = 'SuggestionId')
BEGIN
    ALTER TABLE [dbo].[ComplaintLogs]
    ADD [SuggestionId] int NULL;
    PRINT 'Added SuggestionId column to ComplaintLogs';
END
GO

-- Make ComplaintId nullable in ComplaintLogs
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ComplaintLogs]') AND name = 'ComplaintId' AND is_nullable = 0)
BEGIN
    ALTER TABLE [dbo].[ComplaintLogs]
    ALTER COLUMN [ComplaintId] int NULL;
    PRINT 'Made ComplaintId nullable in ComplaintLogs';
END
GO

-- Create Suggestions table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Suggestions')
BEGIN
    CREATE TABLE [dbo].[Suggestions] (
        [Id] int IDENTITY(1,1) NOT NULL,
        [TicketId] nvarchar(max) NOT NULL,
        [Subject] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [CitizenId] nvarchar(450) NOT NULL,
        [SubCategoryId] int NOT NULL,
        [ProvinceId] int NOT NULL,
        [DistrictId] int NOT NULL,
        [TehsilId] int NOT NULL,
        [AttachmentPath] nvarchar(max) NULL,
        [IsImportant] bit NOT NULL DEFAULT 0,
        [Status] int NOT NULL DEFAULT 0,
        [Priority] int NOT NULL DEFAULT 0,
        [CreatedAt] datetime2 NOT NULL,
        [ResolvedAt] datetime2 NULL,
        [ResponseNote] nvarchar(max) NULL,
        [ResponseAttachmentPath] nvarchar(max) NULL,
        CONSTRAINT [PK_Suggestions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Suggestions_AspNetUsers_CitizenId] FOREIGN KEY ([CitizenId]) REFERENCES [AspNetUsers]([Id]),
        CONSTRAINT [FK_Suggestions_Categories_SubCategoryId] FOREIGN KEY ([SubCategoryId]) REFERENCES [Categories]([Id]),
        CONSTRAINT [FK_Suggestions_Locations_ProvinceId] FOREIGN KEY ([ProvinceId]) REFERENCES [Locations]([Id]),
        CONSTRAINT [FK_Suggestions_Locations_DistrictId] FOREIGN KEY ([DistrictId]) REFERENCES [Locations]([Id]),
        CONSTRAINT [FK_Suggestions_Locations_TehsilId] FOREIGN KEY ([TehsilId]) REFERENCES [Locations]([Id])
    );
    PRINT 'Created Suggestions table';
END
GO

-- Create indexes for Suggestions
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Suggestions_CitizenId')
BEGIN
    CREATE INDEX [IX_Suggestions_CitizenId] ON [dbo].[Suggestions]([CitizenId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Suggestions_SubCategoryId')
BEGIN
    CREATE INDEX [IX_Suggestions_SubCategoryId] ON [dbo].[Suggestions]([SubCategoryId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Suggestions_ProvinceId')
BEGIN
    CREATE INDEX [IX_Suggestions_ProvinceId] ON [dbo].[Suggestions]([ProvinceId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Suggestions_DistrictId')
BEGIN
    CREATE INDEX [IX_Suggestions_DistrictId] ON [dbo].[Suggestions]([DistrictId]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Suggestions_TehsilId')
BEGIN
    CREATE INDEX [IX_Suggestions_TehsilId] ON [dbo].[Suggestions]([TehsilId]);
END
GO

PRINT 'Migration completed successfully!';
GO


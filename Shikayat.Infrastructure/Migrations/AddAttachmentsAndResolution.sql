-- Migration: AddAttachmentsAndResolution
-- Run this script on your SQL Server database to add the new columns
-- Database: ShikayatDb

USE [ShikayatDb];
GO

-- Add AttachmentPath column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Complaints]') AND name = 'AttachmentPath')
BEGIN
    ALTER TABLE [dbo].[Complaints]
    ADD [AttachmentPath] nvarchar(max) NULL;
    PRINT 'Added AttachmentPath column';
END
ELSE
BEGIN
    PRINT 'AttachmentPath column already exists';
END
GO

-- Add ResolutionNote column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Complaints]') AND name = 'ResolutionNote')
BEGIN
    ALTER TABLE [dbo].[Complaints]
    ADD [ResolutionNote] nvarchar(max) NULL;
    PRINT 'Added ResolutionNote column';
END
ELSE
BEGIN
    PRINT 'ResolutionNote column already exists';
END
GO

-- Add ResolutionAttachmentPath column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Complaints]') AND name = 'ResolutionAttachmentPath')
BEGIN
    ALTER TABLE [dbo].[Complaints]
    ADD [ResolutionAttachmentPath] nvarchar(max) NULL;
    PRINT 'Added ResolutionAttachmentPath column';
END
ELSE
BEGIN
    PRINT 'ResolutionAttachmentPath column already exists';
END
GO

PRINT 'Migration completed successfully!';
GO


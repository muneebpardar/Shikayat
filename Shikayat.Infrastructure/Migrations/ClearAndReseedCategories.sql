-- Script to Clear and Reseed Categories
-- This will delete all existing categories and allow the DbInitializer to reseed from categories.json
-- WARNING: This will delete all existing categories and any complaints/suggestions linked to them
-- Make sure to backup your database before running this script

USE ShikayatDb;
GO

-- Disable foreign key constraints temporarily
ALTER TABLE Complaints NOCHECK CONSTRAINT FK_Complaints_Categories_SubCategoryId;
ALTER TABLE Suggestions NOCHECK CONSTRAINT FK_Suggestions_Categories_SubCategoryId;
GO

-- Delete all categories (subcategories first due to foreign key, then parent categories)
DELETE FROM Categories WHERE ParentId IS NOT NULL;
DELETE FROM Categories WHERE ParentId IS NULL;
GO

-- Re-enable foreign key constraints
ALTER TABLE Complaints CHECK CONSTRAINT FK_Complaints_Categories_SubCategoryId;
ALTER TABLE Suggestions CHECK CONSTRAINT FK_Suggestions_Categories_SubCategoryId;
GO

-- Note: After running this script, restart your application and the DbInitializer will reseed categories from categories.json


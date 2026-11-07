
-- Run this first, then CategoryStoreProcedure.sql, MediaStoreProcedure.sql, ReviewStoreProcedure.sql,
-- PhotoStoreProcedure.sql, QuoteStoreProcedure.sql and StatusStoreProcedure.sql (all in this folder).
-- Safe to re-run: tables are only created if missing.

-- CATEGORIES
IF OBJECT_ID('dbo.MediaCategories', 'U') IS NULL
CREATE TABLE MediaCategories (
    Id INT IDENTITY(1,1) CONSTRAINT PK_MediaCategories PRIMARY KEY,
    Name VARCHAR(255) NOT NULL
);
GO

-- MEDIA ITEMS
IF OBJECT_ID('dbo.MediaItems', 'U') IS NULL
CREATE TABLE MediaItems (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_MediaItems_Id DEFAULT NEWID() CONSTRAINT PK_MediaItems PRIMARY KEY,
    MediaTitle NVARCHAR(MAX) NULL,
    CategoryId INT NOT NULL CONSTRAINT FK_Media_Category REFERENCES MediaCategories(Id),
    CoverPhotoUrl VARCHAR(500) NULL,
    AverageRating DECIMAL(4, 2) NULL CONSTRAINT DF_MediaItems_AverageRating DEFAULT 0,
    Status VARCHAR(50) NULL,
    MediaLink VARCHAR(MAX) NULL,
    DateCreated DATETIME NULL CONSTRAINT DF_MediaItems_DateCreated DEFAULT GETDATE(),
    DateUpdated DATETIME NULL,
    Deleted BIT NULL CONSTRAINT DF_MediaItems_Deleted DEFAULT 0
);
GO

-- PHOTOS
IF OBJECT_ID('dbo.MediaPhoto', 'U') IS NULL
CREATE TABLE MediaPhoto (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_MediaPhoto_Id DEFAULT NEWID() CONSTRAINT PK_MediaPhoto PRIMARY KEY,
    MediaId UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_MediaPhoto_Media REFERENCES MediaItems(Id),
    PhotoUrl VARCHAR(500) NULL
);
GO

-- QUOTES
IF OBJECT_ID('dbo.MediaQuotes', 'U') IS NULL
CREATE TABLE MediaQuotes (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_MediaQuotes_Id DEFAULT NEWID() CONSTRAINT PK_MediaQuotes PRIMARY KEY,
    MediaId UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_MediaQuotes_Media REFERENCES MediaItems(Id),
    QuoteFrom VARCHAR(255) NULL,
    QuoteText VARCHAR(MAX) NULL
);
GO

-- REVIEWS
IF OBJECT_ID('dbo.Reviews', 'U') IS NULL
CREATE TABLE Reviews (
    Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Reviews_Id DEFAULT NEWID() CONSTRAINT PK_Reviews PRIMARY KEY,
    MediaId UNIQUEIDENTIFIER NOT NULL CONSTRAINT FK_Comments_Media REFERENCES MediaItems(Id),
    Rating INT NULL,
    Content VARCHAR(MAX) NULL,
    DateCreated DATETIME NULL CONSTRAINT DF_Reviews_DateCreated DEFAULT GETDATE()
);
GO


-- FIX: decimal(3,2) tops out at 9.99, so a 10/10 average overflowed. Widen existing databases.
ALTER TABLE MediaItems ALTER COLUMN AverageRating DECIMAL(4, 2) NULL;
GO

-- Statuses (Planning, Consuming...), editable in Settings. MediaItems.Status stores the name.
IF OBJECT_ID('dbo.MediaStatuses', 'U') IS NULL
CREATE TABLE MediaStatuses (
    Id INT IDENTITY(1,1) CONSTRAINT PK_MediaStatuses PRIMARY KEY,
    Name VARCHAR(50) NOT NULL CONSTRAINT UQ_MediaStatuses_Name UNIQUE,
    SortOrder INT NOT NULL CONSTRAINT DF_MediaStatuses_SortOrder DEFAULT 0
);
GO

-- Seed: the statuses that used to be hard-coded (in the filter's order), plus any others in use
IF NOT EXISTS (SELECT 1 FROM MediaStatuses)
BEGIN
    INSERT INTO MediaStatuses (Name, SortOrder)
    VALUES ('Paused', 0), ('Dropped', 1), ('Planning', 2), ('Completed', 3), ('Consuming', 4);

    INSERT INTO MediaStatuses (Name, SortOrder)
    SELECT DISTINCT m.Status, 100
    FROM MediaItems m
    WHERE m.Status IS NOT NULL AND m.Status <> ''
      AND NOT EXISTS (SELECT 1 FROM MediaStatuses s WHERE s.Name = m.Status);
END
GO

-- Archive: hidden from the normal lists, kept intact, can be restored
IF COL_LENGTH('dbo.MediaItems', 'Archived') IS NULL
    ALTER TABLE MediaItems ADD Archived BIT NOT NULL CONSTRAINT DF_MediaItems_Archived DEFAULT 0;
GO
IF COL_LENGTH('dbo.MediaItems', 'DateArchived') IS NULL
    ALTER TABLE MediaItems ADD DateArchived DATETIME NULL;
GO

-- Category color (hex, e.g. #1e90ff), used as the card background
IF COL_LENGTH('dbo.MediaCategories', 'Color') IS NULL
    ALTER TABLE MediaCategories ADD Color VARCHAR(7) NULL;
GO

-- Seed colors that used to be hard-coded in the frontend, so existing cards look the same
UPDATE MediaCategories
SET Color = CASE LOWER(Name)
        WHEN 'anime' THEN '#a1a1a1'
        WHEN 'movies' THEN '#1e90ff'
        WHEN 'music' THEN '#32cd32'
        WHEN 'books' THEN '#ffa500'
        WHEN 'youtube video' THEN '#ff5cad'
        ELSE '#d43b0c'
    END
WHERE Color IS NULL;
GO

-- Notable quotes display order (0 = first in the carousel)
IF COL_LENGTH('dbo.MediaQuotes', 'SortOrder') IS NULL
    ALTER TABLE MediaQuotes ADD SortOrder INT NOT NULL CONSTRAINT DF_MediaQuotes_SortOrder DEFAULT 0;
GO

-- Photo display order (0 = first after the cover photo)
IF COL_LENGTH('dbo.MediaPhoto', 'SortOrder') IS NULL
    ALTER TABLE MediaPhoto ADD SortOrder INT NOT NULL CONSTRAINT DF_MediaPhoto_SortOrder DEFAULT 0;
GO

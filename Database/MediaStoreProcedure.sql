
-- All GET procedures skip soft-deleted items (Deleted = 1).
-- Archived items (Archived = 1) only appear when a list is asked for them (@Archived = 1);
-- GetMediaById returns an item either way.

-- GET ALL MEDIA
CREATE OR ALTER PROCEDURE GetAllMedia
    @Archived BIT = 0
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE ISNULL(MediaItems.Deleted, 0) = 0
        AND ISNULL(MediaItems.Archived, 0) = @Archived
    ORDER BY MediaItems.DateUpdated DESC; -- Default sorting by date updated
END
GO

-- GET ALL MEDIA SORTED BY CATEGORY
CREATE OR ALTER PROCEDURE GetAllMediaSortedByCategory
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE ISNULL(MediaItems.Deleted, 0) = 0
        AND ISNULL(MediaItems.Archived, 0) = 0
    ORDER BY MediaCategories.Name, MediaItems.MediaTitle
END
GO


-- GET ALL MEDIA BY SEARCH TERM
CREATE OR ALTER PROCEDURE SearchMediaByName
    @SearchTerm NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.MediaTitle LIKE '%' + @SearchTerm + '%'
        AND ISNULL(MediaItems.Deleted, 0) = 0
        AND ISNULL(MediaItems.Archived, 0) = 0
    ORDER BY MediaItems.MediaTitle
END
GO

-- GET MEDIA BY ID
CREATE OR ALTER PROCEDURE GetMediaById
    @MediaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.Id = @MediaId
        AND ISNULL(MediaItems.Deleted, 0) = 0
END
GO


-- GET MEDIA BY CategoryId
CREATE OR ALTER PROCEDURE GetMediaByCategoryId
    @CategoryId INT,
    @Archived BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.CategoryId = @CategoryId
        AND ISNULL(MediaItems.Deleted, 0) = 0
        AND ISNULL(MediaItems.Archived, 0) = @Archived
    ORDER BY MediaItems.DateUpdated DESC
END
GO


-- GET MEDIA BY CategoryName
CREATE OR ALTER PROCEDURE GetMediaByCategoryName
    @CategoryName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaCategories.Name = @CategoryName
        AND ISNULL(MediaItems.Deleted, 0) = 0
        AND ISNULL(MediaItems.Archived, 0) = 0
    ORDER BY MediaItems.DateUpdated DESC
END
GO


-- GET Media by Status ->
CREATE OR ALTER PROCEDURE GetMediaItemByStatus
    @Status NVARCHAR(50),
    @Archived BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.Status = @Status
        AND ISNULL(MediaItems.Deleted, 0) = 0
        AND ISNULL(MediaItems.Archived, 0) = @Archived
    ORDER BY MediaItems.DateUpdated DESC
END
GO


-- Get Media by CategoryId and Status -> GetMediaItemByCategoryIdAndStatus
CREATE OR ALTER PROCEDURE GetMediaItemByCategoryIdAndStatus
    @CategoryId INT,
    @Status NVARCHAR(50),
    @Archived BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.CategoryId = @CategoryId AND MediaItems.Status = @Status
        AND ISNULL(MediaItems.Deleted, 0) = 0
        AND ISNULL(MediaItems.Archived, 0) = @Archived
    ORDER BY MediaItems.DateUpdated DESC
END
GO



-- POST New Media Item -> InsertMediaItem
CREATE OR ALTER PROCEDURE InsertMediaItem
    @MediaTitle NVARCHAR(200),
    @CategoryId INT,
    @CoverPhotoUrl NVARCHAR(500),
    @Status NVARCHAR(50),
    @MediaLink NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO MediaItems (MediaTitle, CategoryId, CoverPhotoUrl, Status, MediaLink, DateCreated, DateUpdated)
    VALUES (@MediaTitle, @CategoryId, @CoverPhotoUrl, @Status, @MediaLink, GETDATE(), GETDATE())
END
GO


-- PUT -> UPDATE MEDIA BY ID
CREATE OR ALTER PROCEDURE UpdateMediaItemById
    @Id UNIQUEIDENTIFIER,
    @MediaTitle NVARCHAR(200),
    @CategoryId INT,
    @CoverPhotoUrl NVARCHAR(500),
    @Status NVARCHAR(50),
    @MediaLink NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MediaItems
    SET MediaTitle = @MediaTitle,
        CategoryId = @CategoryId,
        CoverPhotoUrl = @CoverPhotoUrl,
        Status = @Status,
        MediaLink = @MediaLink,
        DateUpdated = GETDATE()
    WHERE Id = @Id;
END
GO


-- PUT -> UPDATE MEDIA STATUS BY ID
CREATE OR ALTER PROCEDURE UpdateMediaStatusById
    @MediaId UNIQUEIDENTIFIER,
    @Status NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MediaItems
    SET Status = @Status
    WHERE Id = @MediaId;
END
GO


-- Rating is no longer set by the client; ReviewStoreProcedure.sql recalculates it.
DROP PROCEDURE IF EXISTS UpdateMediaRatingById;
GO

-- DELETE -> DELETE MEDIA BY ID
-- Soft delete: reviews/photos/quotes reference the item without cascade, so a hard delete fails.
CREATE OR ALTER PROCEDURE DeleteMediaItemById
    @MediaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MediaItems
    SET Deleted = 1,
        DateUpdated = GETDATE()
    WHERE Id = @MediaId;
END
GO


-- PUT -> ARCHIVE / UNARCHIVE MEDIA BY ID
-- Archiving hides an item from the normal lists without touching its reviews, photos or rating.
-- DateUpdated is left alone so it returns to the same place in the list when unarchived.
-- Returns the item (empty if it doesn't exist).
CREATE OR ALTER PROCEDURE SetMediaArchived
    @MediaId UNIQUEIDENTIFIER,
    @Archived BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE MediaItems
    SET Archived = @Archived,
        DateArchived = CASE WHEN @Archived = 1 THEN GETDATE() ELSE NULL END
    WHERE Id = @MediaId AND ISNULL(Deleted, 0) = 0;

    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.Id = @MediaId AND ISNULL(MediaItems.Deleted, 0) = 0
END
GO


-- PUT -> BULK UPDATE (used by the Archive's bulk edit)
-- @Ids is a JSON array of media ids. Each change is optional (NULL = leave as is):
-- archive/unarchive, set status, set category, delete (soft).
-- DateUpdated is left alone so items keep their place in the list.
-- Returns how many items were updated.
CREATE OR ALTER PROCEDURE BulkUpdateMediaItems
    @Ids NVARCHAR(MAX),
    @Archived BIT = NULL,
    @Status VARCHAR(50) = NULL,
    @CategoryId INT = NULL,
    @Deleted BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE MediaItems
    SET Archived = ISNULL(@Archived, Archived),
        DateArchived = CASE
            WHEN @Archived = 1 AND ISNULL(Archived, 0) = 0 THEN GETDATE()
            WHEN @Archived = 0 THEN NULL
            ELSE DateArchived END,
        Status = ISNULL(@Status, Status),
        CategoryId = ISNULL(@CategoryId, CategoryId),
        Deleted = ISNULL(@Deleted, Deleted)
    WHERE Id IN (SELECT TRY_CAST(value AS UNIQUEIDENTIFIER) FROM OPENJSON(@Ids))
      AND ISNULL(Deleted, 0) = 0;

    SELECT @@ROWCOUNT AS UpdatedCount;
END
GO

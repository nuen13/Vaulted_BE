


/*
    Author: Aiden 
    Desctiption: Returns all media items in the database.
    Date: 27/03/2026
*/
ALTER PROCEDURE GetAllMedia
AS
BEGIN
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    LEFT JOIN MediaCategories ON MediaItems.CategoryID = MediaCategories.Id
    WHERE MediaItems.Deleted = 0; -- Exclude deleted items
END
GO

SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
LEFT JOIN MediaCategories ON MediaItems.CategoryID = MediaCategories.Id;


-- Return all media items by their category name

ALTER PROCEDURE GetMediaByCategory
    @Category NVARCHAR(50)
AS
BEGIN
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    LEFT JOIN MediaCategories ON MediaItems.CategoryID = MediaCategories.Id
    WHERE MediaCategories.Name = @Category;
END


EXEC GetMediaByCategory @Category = "movies"


-- Return all media items and category name by their category id

ALTER PROCEDURE GetMediaByCategoryId
    @CategoryId INT
AS
BEGIN
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    LEFT JOIN MediaCategories ON MediaItems.CategoryID = MediaCategories.Id
    WHERE MediaCategories.Id = @CategoryId;
END


EXEC GetMediaByCategoryId @CategoryId = 1





-- INSERT INTO MediaItems -> id, mediaTitle, categoryid, coverPhotoUrl,AverageScore = 0,status, mediaLink, dateCreated, DateUpdated, deleted = 0 

ALTER PROCEDURE InsertMediaItem
    @MediaTitle NVARCHAR(255),
    @CategoryId INT,
    @CoverPhotoUrl NVARCHAR(255),
    @Status NVARCHAR(50),
    @MediaLink NVARCHAR(255)
AS
BEGIN
    INSERT INTO MediaItems (MediaTitle, CategoryID, CoverPhotoUrl, AverageScore, Status, MediaLink, DateCreated, DateUpdated, Deleted)
    VALUES (@MediaTitle, @CategoryId, @CoverPhotoUrl, 0, @Status, @MediaLink, GETDATE(), GETDATE(), 0);
END
GO



-- Update media item by id
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE UpdateMediaItemById
    @Id UNIQUEIDENTIFIER,
    @MediaTitle NVARCHAR(255),
    @CategoryId INT,
    @CoverPhotoUrl NVARCHAR(255),
    @Status NVARCHAR(50),
    @MediaLink NVARCHAR(255)
AS
BEGIN 
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    UPDATE MediaItems
    SET MediaTitle = @MediaTitle,
        CategoryID = @CategoryId,
        CoverPhotoUrl = @CoverPhotoUrl,
        Status = @Status,
        MediaLink = @MediaLink,
        DateUpdated = GETDATE()
    WHERE Id = @Id;
END
GO




/*

    Get all media and sort them by their category name

*/

CREATE OR ALTER PROCEDURE GetAllMediaSortedByCategory
AS
BEGIN
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    LEFT JOIN MediaCategories ON MediaItems.CategoryID = MediaCategories.Id
    WHERE MediaItems.Deleted = 0
    ORDER BY MediaCategories.Name;
END
GO

SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
LEFT JOIN MediaCategories ON MediaItems.CategoryID = MediaCategories.Id
WHERE MediaItems.Deleted = 0
ORDER BY MediaCategories.Name;

/*
    Search Media Item by title 
*/

CREATE OR ALTER PROCEDURE SearchMediaByTitle
    @SearchTerm NVARCHAR(255)
AS
BEGIN
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    LEFT JOIN MediaCategories ON MediaItems.CategoryID = MediaCategories.Id
    WHERE MediaItems.MediaTitle LIKE '%' + @SearchTerm + '%'
    AND MediaItems.Deleted = 0;
END




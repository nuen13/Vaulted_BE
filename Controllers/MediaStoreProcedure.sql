

-- GET ALL MEDIA
CREATE OR ALTER PROCEDURE GetAllMedia
AS 
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM MediaItems
END

-- GET ALL MEDIA SORTED BY CATEGORY
CREATE OR ALTER PROCEDURE GetAllMediaSortedByCategory
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    ORDER BY MediaCategories.Name, MediaItems.MediaTitle
END


-- GET ALL MEDIA BY SEARCH TERM
CREATE OR ALTER PROCEDURE SearchMediaByName
    @SearchTerm NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.MediaTitle LIKE '%' + @SearchTerm + '%'
    ORDER BY MediaItems.MediaTitle
END


-- GET MEDIA BY CategoryId
CREATE OR ALTER PROCEDURE GetMediaByCategoryId
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.CategoryId = @CategoryId
    ORDER BY MediaItems.DateUpdated DESC
END


-- GET MEDIA BY CategoryName
CREATE OR ALTER PROCEDURE GetMediaByCategoryName
    @CategoryName NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaCategories.Name = @CategoryName
    ORDER BY MediaItems.DateUpdated DESC
END


-- GET Media by Status -> 
CREATE OR ALTER PROCEDURE GetMediaItemByStatus
    @Status NVARCHAR(50)
AS
BEGIN
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.Status = @Status
END
GO 


-- Get Media by CategoryId and Status -> GetMediaItemByCategoryIdAndStatus
CREATE OR ALTER PROCEDURE GetMediaItemByCategoryIdAndStatus
    @CategoryId INT,
    @Status NVARCHAR(50)
AS
BEGIN
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.CategoryId = @CategoryId AND MediaItems.Status = @Status
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


-- PUT -> UPDATE MEDIA RATING BY ID
CREATE OR ALTER PROCEDURE UpdateMediaRatingById 
    @MediaId UNIQUEIDENTIFIER,
    @AverageRating FLOAT
AS
BEGIN
    SET NOCOUNT ON; 
    UPDATE MediaItems
    SET AverageRating = @AverageRating
    WHERE Id = @MediaId;
END




GO

-- DELETE -> DELETE MEDIA BY ID
CREATE OR ALTER PROCEDURE DeleteMediaItemById
    @MediaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM MediaItems
    WHERE Id = @MediaId;
END


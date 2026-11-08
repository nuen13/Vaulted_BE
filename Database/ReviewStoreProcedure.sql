
-- Adding, updating, or deleting a review recalculates MediaItems.AverageRating
-- in the same transaction and returns the new value to the caller.
--
-- Every review belongs to the signed-in user who wrote it (Reviews.UserId), and only its
-- author can change or delete it. Old reviews without an author can be changed by anyone.
-- Errors are raised with THROW so the API can map them: 50404 = not found, 50403 = not yours.


-- HELPER -> RECALCULATE AVERAGE RATING FOR ONE MEDIA ITEM
CREATE OR ALTER PROCEDURE RecalculateMediaRating
    @MediaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE MediaItems
    SET AverageRating = ISNULL((
            SELECT CAST(AVG(CAST(Rating AS DECIMAL(6, 2))) AS DECIMAL(4, 2))
            FROM Reviews
            WHERE MediaId = @MediaId AND Rating IS NOT NULL
        ), 0)
    WHERE Id = @MediaId;
END
GO


-- GET -> GET ALL REVIEWS BY MEDIA ID (with the author's name and picture)
CREATE OR ALTER PROCEDURE GetReviewsByMediaId
    @MediaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    SELECT r.Id, r.MediaId, r.Rating, r.Content, r.DateCreated, r.UserId,
           u.DisplayName AS AuthorName, u.PictureUrl AS AuthorPictureUrl
    FROM Reviews r
    LEFT JOIN Users u ON r.UserId = u.Id
    WHERE r.MediaId = @MediaId
    ORDER BY r.DateCreated DESC; -- Default sorting by date created
END
GO


-- HELPER -> ONE REVIEW WITH ITS AUTHOR AND THE MEDIA'S AVERAGE RATING
CREATE OR ALTER PROCEDURE GetReviewWithRating
    @ReviewId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT r.Id, r.MediaId, r.Rating, r.Content, r.DateCreated, r.UserId,
           u.DisplayName AS AuthorName, u.PictureUrl AS AuthorPictureUrl,
           m.AverageRating
    FROM Reviews r
    INNER JOIN MediaItems m ON r.MediaId = m.Id
    LEFT JOIN Users u ON r.UserId = u.Id
    WHERE r.Id = @ReviewId;
END
GO


-- POST -> ADD REVIEW TO MEDIA
-- Returns the created review plus the media's new AverageRating.
CREATE OR ALTER PROCEDURE AddReviewToMedia
    @MediaId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @Rating INT,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM MediaItems WHERE Id = @MediaId AND ISNULL(Deleted, 0) = 0)
        THROW 50404, 'Media not found.', 1;

    DECLARE @ReviewId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRANSACTION;

    INSERT INTO Reviews (Id, MediaId, Rating, Content, DateCreated, UserId)
    VALUES (@ReviewId, @MediaId, @Rating, @Content, GETDATE(), @UserId);

    EXEC RecalculateMediaRating @MediaId;

    COMMIT TRANSACTION;

    EXEC GetReviewWithRating @ReviewId;
END
GO

-- Unused duplicate of AddReviewToMedia
DROP PROCEDURE IF EXISTS AddReviewToMediaByMediaId;
GO


-- PUT -> UPDATE REVIEW BY ID
-- Returns the updated review plus the media's new AverageRating.
CREATE OR ALTER PROCEDURE UpdateReviewById
    @ReviewId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @Rating INT,
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MediaId UNIQUEIDENTIFIER, @AuthorId UNIQUEIDENTIFIER;
    SELECT @MediaId = MediaId, @AuthorId = UserId FROM Reviews WHERE Id = @ReviewId;

    IF @MediaId IS NULL
        THROW 50404, 'Review not found.', 1;
    IF @AuthorId IS NOT NULL AND @AuthorId <> @UserId
        THROW 50403, 'Only the person who wrote this review can change it.', 1;

    BEGIN TRANSACTION;

    UPDATE Reviews
    SET Content = @Content, Rating = @Rating
    WHERE Id = @ReviewId;

    EXEC RecalculateMediaRating @MediaId;

    COMMIT TRANSACTION;

    EXEC GetReviewWithRating @ReviewId;
END
GO

-- DELETE -> DELETE REVIEW BY ID
-- Returns the media id and its new AverageRating.
-- The author or an admin can delete it.
CREATE OR ALTER PROCEDURE DeleteReviewById
    @ReviewId UNIQUEIDENTIFIER,
    @UserId UNIQUEIDENTIFIER,
    @IsAdmin BIT = 0
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MediaId UNIQUEIDENTIFIER, @AuthorId UNIQUEIDENTIFIER;
    SELECT @MediaId = MediaId, @AuthorId = UserId FROM Reviews WHERE Id = @ReviewId;

    IF @MediaId IS NULL
        THROW 50404, 'Review not found.', 1;
    IF @IsAdmin = 0 AND @AuthorId IS NOT NULL AND @AuthorId <> @UserId
        THROW 50403, 'Only the person who wrote this review can delete it.', 1;

    BEGIN TRANSACTION;

    DELETE FROM Reviews
    WHERE Id = @ReviewId;

    EXEC RecalculateMediaRating @MediaId;

    COMMIT TRANSACTION;

    SELECT Id AS MediaId, AverageRating
    FROM MediaItems
    WHERE Id = @MediaId;
END
GO

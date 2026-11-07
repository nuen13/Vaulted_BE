
-- Adding, updating, or deleting a review recalculates MediaItems.AverageRating
-- in the same transaction and returns the new value to the caller.


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


-- GET -> GET ALL REVIEWS BY MEDIA ID
CREATE OR ALTER PROCEDURE GetReviewsByMediaId
    @MediaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    SELECT *
    FROM Reviews
    WHERE MediaId = @MediaId
    ORDER BY DateCreated DESC; -- Default sorting by date created
END
GO


-- POST -> ADD REVIEW TO MEDIA
-- Returns the created review plus the media's new AverageRating.
CREATE OR ALTER PROCEDURE AddReviewToMedia
    @MediaId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @Rating INT
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core
    SET XACT_ABORT ON;

    DECLARE @ReviewId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRANSACTION;

    INSERT INTO Reviews (Id, MediaId, Rating, Content, DateCreated)
    VALUES (@ReviewId, @MediaId, @Rating, @Content, GETDATE());

    EXEC RecalculateMediaRating @MediaId;

    COMMIT TRANSACTION;

    SELECT Reviews.*, MediaItems.AverageRating
    FROM Reviews
    INNER JOIN MediaItems ON Reviews.MediaId = MediaItems.Id
    WHERE Reviews.Id = @ReviewId;
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
    @Rating INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MediaId UNIQUEIDENTIFIER = (SELECT MediaId FROM Reviews WHERE Id = @ReviewId);

    BEGIN TRANSACTION;

    UPDATE Reviews
    SET Content = @Content, Rating = @Rating
    WHERE Id = @ReviewId;

    EXEC RecalculateMediaRating @MediaId;

    COMMIT TRANSACTION;

    SELECT Reviews.*, MediaItems.AverageRating
    FROM Reviews
    INNER JOIN MediaItems ON Reviews.MediaId = MediaItems.Id
    WHERE Reviews.Id = @ReviewId;
END
GO

-- DELETE -> DELETE REVIEW BY ID
-- Returns the media id and its new AverageRating.
CREATE OR ALTER PROCEDURE DeleteReviewById
    @ReviewId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MediaId UNIQUEIDENTIFIER = (SELECT MediaId FROM Reviews WHERE Id = @ReviewId);

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

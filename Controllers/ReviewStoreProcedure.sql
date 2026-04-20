



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

-

-- POST -> ADD REVIEW TO MEDIA
CREATE OR ALTER PROCEDURE AddReviewToMedia
    @MediaId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @Rating INT
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    INSERT INTO Reviews (MediaId, Rating, Content, DateCreated)
    VALUES (@MediaId, @Rating, @Content, GETDATE());
END

GO

-- POST -> ADD REVIEW TO MEDIA BY MediaID
CREATE OR ALTER PROCEDURE AddReviewToMediaByMediaId
    @MediaId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @Rating INT
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    INSERT INTO Reviews (MediaId, Rating, Content, DateCreated)
    VALUES (@MediaId, @Rating, @Content, GETDATE());
END


-- PUT -> UPDATE REVIEW BY ID
CREATE OR ALTER PROCEDURE UpdateReviewById
    @ReviewId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @Rating INT
AS
BEGIN
    SET NOCOUNT ON; 

    UPDATE Reviews
    SET Content = @Content, Rating = @Rating
    WHERE Id = @ReviewId;
END
GO

-- DELETE -> DELETE REVIEW BY ID
CREATE OR ALTER PROCEDURE DeleteReviewById
    @ReviewId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM Reviews
    WHERE Id = @ReviewId;
END
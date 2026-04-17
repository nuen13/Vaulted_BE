

/*
        To do:

        // GET: Get all comments of a media with pagination
        // GET: Get all comments of a media with pagination and sorting by date created
        // GET: Get all comments of a media with pagination and sorting by score
        // GET: Get all comments of a media with pagination and sorting by date created and score
        // GET: Get all comments of a media with pagination and sorting by date created and score and filter by score range

        // PUT: Edit a comment
        // DELETE: Delete a comment

*/ 




/*
    POST: Add Comment to a media
*/
CREATE OR ALTER PROCEDURE AddCommentToMedia
    @MediaId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @Rating INT
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    INSERT INTO Comments (MediaId, Rating, Content, DateCreated)
    VALUES (@MediaId, @Rating, @Content, GETDATE());
END

GO


/*
    GET: Get all comments of a media
*/

CREATE OR ALTER PROCEDURE GetCommentsByMediaId
    @MediaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    SELECT *
    FROM Comments
    WHERE MediaId = @MediaId
    ORDER BY DateCreated DESC; -- Default sorting by date created
END


-- Update comment content and rating by comment id
CREATE OR ALTER PROCEDURE UpdateCommentById
    @CommentId UNIQUEIDENTIFIER,
    @Content NVARCHAR(MAX),
    @Rating INT
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    UPDATE Comments
    SET Content = @Content, Rating = @Rating
    WHERE Id = @CommentId;
END
GO
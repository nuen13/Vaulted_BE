
-- Extra photos for a media item. The cover photo stays on MediaItems.CoverPhotoUrl;
-- these come after it, ordered by SortOrder.


-- GET -> GET ALL PHOTOS BY MEDIA ID
CREATE OR ALTER PROCEDURE GetPhotosByMediaId
    @MediaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    SELECT Id, MediaId, PhotoUrl, SortOrder
    FROM MediaPhoto
    WHERE MediaId = @MediaId
    ORDER BY SortOrder, Id;
END
GO


-- POST -> ADD PHOTO TO MEDIA (appended to the end)
-- Returns the created photo.
CREATE OR ALTER PROCEDURE AddPhotoToMedia
    @MediaId UNIQUEIDENTIFIER,
    @PhotoUrl VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @PhotoId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRANSACTION;

    INSERT INTO MediaPhoto (Id, MediaId, PhotoUrl, SortOrder)
    SELECT @PhotoId, @MediaId, @PhotoUrl, ISNULL(MAX(SortOrder) + 1, 0)
    FROM MediaPhoto WITH (UPDLOCK, HOLDLOCK)
    WHERE MediaId = @MediaId;

    COMMIT TRANSACTION;

    SELECT Id, MediaId, PhotoUrl, SortOrder
    FROM MediaPhoto
    WHERE Id = @PhotoId;
END
GO


-- PUT -> REORDER PHOTOS
-- @PhotoIds is a JSON array of photo ids in the new order, e.g. ["id1","id2"].
-- Ids that don't belong to @MediaId are ignored. Returns the photos in their new order.
CREATE OR ALTER PROCEDURE ReorderMediaPhotos
    @MediaId UNIQUEIDENTIFIER,
    @PhotoIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE MediaPhoto
    SET SortOrder = CAST(j.[key] AS INT)
    FROM MediaPhoto
    INNER JOIN OPENJSON(@PhotoIds) j ON MediaPhoto.Id = TRY_CAST(j.value AS UNIQUEIDENTIFIER)
    WHERE MediaPhoto.MediaId = @MediaId;

    EXEC GetPhotosByMediaId @MediaId;
END
GO


-- PUT -> SET PHOTO AS COVER
-- Swaps the photo with the current cover: the photo becomes the cover, and the old cover
-- takes the photo's place in the gallery (same position). If there was no cover, the photo
-- just moves to the cover. DateUpdated is left alone so the media list order doesn't change.
-- Returns the media id and new cover (empty if the photo didn't exist).
CREATE OR ALTER PROCEDURE SetPhotoAsCover
    @PhotoId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @MediaId UNIQUEIDENTIFIER, @NewCover VARCHAR(500), @OldCover VARCHAR(500);

    BEGIN TRANSACTION;

    SELECT @MediaId = MediaId, @NewCover = PhotoUrl
    FROM MediaPhoto WITH (UPDLOCK)
    WHERE Id = @PhotoId;

    IF @MediaId IS NOT NULL
    BEGIN
        SELECT @OldCover = CoverPhotoUrl
        FROM MediaItems WITH (UPDLOCK)
        WHERE Id = @MediaId;

        UPDATE MediaItems
        SET CoverPhotoUrl = @NewCover
        WHERE Id = @MediaId;

        IF NULLIF(LTRIM(RTRIM(@OldCover)), '') IS NULL
            DELETE FROM MediaPhoto WHERE Id = @PhotoId;
        ELSE
            UPDATE MediaPhoto SET PhotoUrl = @OldCover WHERE Id = @PhotoId;
    END

    COMMIT TRANSACTION;

    -- Always return a result set (empty when the photo doesn't exist) so EF can read it
    SELECT @MediaId AS MediaId, @NewCover AS CoverPhotoUrl
    WHERE @MediaId IS NOT NULL;
END
GO


-- DELETE -> DELETE PHOTO BY ID
-- Returns the deleted photo's media id (empty if it didn't exist).
CREATE OR ALTER PROCEDURE DeletePhotoById
    @PhotoId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM MediaPhoto
    OUTPUT deleted.MediaId
    WHERE Id = @PhotoId;
END
GO


-- Category rows returned to the app: Id, Name, Color, MediaCount (items not soft-deleted)


-- GET -> GET ALL CATEGORIES
CREATE OR ALTER PROCEDURE GetAllCategories
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    SELECT c.Id, c.Name, c.Color,
        (SELECT COUNT(*) FROM MediaItems m WHERE m.CategoryId = c.Id AND ISNULL(m.Deleted, 0) = 0) AS MediaCount
    FROM MediaCategories c
    ORDER BY c.Id;
END
GO


-- POST -> CREATE NEW CATEGORY
-- Returns the created category.
CREATE OR ALTER PROCEDURE AddCategory
    @Name NVARCHAR(50),
    @Color VARCHAR(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO MediaCategories (Name, Color)
    VALUES (@Name, ISNULL(@Color, '#d43b0c'));

    DECLARE @Id INT = SCOPE_IDENTITY();

    SELECT Id, Name, Color, 0 AS MediaCount
    FROM MediaCategories
    WHERE Id = @Id;
END
GO

-- PUT -> UPDATE CATEGORY BY ID (name and color)
-- Returns the updated category (empty if it doesn't exist).
CREATE OR ALTER PROCEDURE UpdateCategoryById
    @CategoryId INT,
    @Name NVARCHAR(50),
    @Color VARCHAR(7) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE MediaCategories
    SET Name = @Name,
        Color = ISNULL(@Color, Color)
    WHERE Id = @CategoryId;

    SELECT c.Id, c.Name, c.Color,
        (SELECT COUNT(*) FROM MediaItems m WHERE m.CategoryId = c.Id AND ISNULL(m.Deleted, 0) = 0) AS MediaCount
    FROM MediaCategories c
    WHERE c.Id = @CategoryId;
END
GO


-- DELETE -> DELETE CATEGORY BY ID
-- Media items reference their category, so a category in use can't simply be deleted.
-- Pass @ReassignToId to move its items (including soft-deleted ones) to another category first.
-- Returns one row: Deleted (1/0) and ItemCount (items that were, or still are, in the category).
-- No row if the category doesn't exist.
CREATE OR ALTER PROCEDURE DeleteCategoryById
    @CategoryId INT,
    @ReassignToId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF NOT EXISTS (SELECT 1 FROM MediaCategories WHERE Id = @CategoryId)
    BEGIN
        SELECT CAST(0 AS BIT) AS Deleted, 0 AS ItemCount WHERE 1 = 0; -- empty result
        RETURN;
    END

    DECLARE @ItemCount INT = (SELECT COUNT(*) FROM MediaItems WHERE CategoryId = @CategoryId);

    IF @ItemCount > 0 AND @ReassignToId IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS Deleted, @ItemCount AS ItemCount;
        RETURN;
    END

    BEGIN TRANSACTION;

    IF @ItemCount > 0
        UPDATE MediaItems
        SET CategoryId = @ReassignToId
        WHERE CategoryId = @CategoryId;

    DELETE FROM MediaCategories
    WHERE Id = @CategoryId;

    COMMIT TRANSACTION;

    SELECT CAST(1 AS BIT) AS Deleted, @ItemCount AS ItemCount;
END
GO

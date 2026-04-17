
-- GET -> GET ALL CATEGORIES
CREATE OR ALTER PROCEDURE GetAllCategories
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    SELECT Id, Name
    FROM MediaCategories
    ORDER BY Id; -- Sort categories alphabetically
END

GO


-- POST -> CREATE NEW CATEGORY
CREATE OR ALTER PROCEDURE AddCategory
    @Name NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO MediaCategories (Name)
    VALUES (@Name);
END

-- PUT -> UPDATE CATEGORY BY ID
CREATE OR ALTER PROCEDURE UpdateCategoryById
    @CategoryId INT,
    @Name NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE MediaCategories
    SET Name = @Name
    WHERE Id = @CategoryId;
END


-- DELETE -> DELETE CATEGORY BY ID
CREATE OR ALTER PROCEDURE DeleteCategoryById
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM MediaCategories
    WHERE Id = @CategoryId;
END
GO
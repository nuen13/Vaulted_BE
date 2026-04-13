
CREATE OR ALTER PROCEDURE GetAllCurrentCategories
AS
BEGIN
    SET NOCOUNT ON;
    -- Stops 'rows affected' messages from messing with EF Core

    SELECT Id, Name
    FROM MediaCategories
    ORDER BY Name;
END

GO


SELECT Id, Name
FROM MediaCategories
ORDER BY Name; 

CREATE OR ALTER PROCEDURE GetAllCategories
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    SELECT CategoryID, CategoryName
    FROM Categories
    ORDER BY CategoryName; -- Sort categories alphabetically
END

GO


SELECT Id, Name
FROM MedieCategories
ORDER BY Name; 
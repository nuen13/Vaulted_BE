
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



CREATE OR ALTER PROCEDURE GetMediaItemByStatus
    @Status NVARCHAR(50)
AS
BEGIN
    SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
    INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
    WHERE MediaItems.Status = @Status
END
GO 

SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
WHERE MediaItems.Status = 'Planning'

-- Msg 208, Level 16, State 1, Line 1
-- Invalid object name 'MediaItems'.

SELECT MediaItems.*, MediaCategories.Name AS CategoryName FROM MediaItems
INNER JOIN MediaCategories ON MediaItems.CategoryId = MediaCategories.Id
WHERE MediaItems.Status = 'Planning'

Select * FROM MediaItems
WHERE Status = 'Planning'


-- Update Score by MediaId
CREATE OR ALTER PROCEDURE UpdateMediaScoreById 
    @MediaId UNIQUEIDENTIFIER,
    @Score FLOAT
AS
BEGIN
    SET NOCOUNT ON; 
    UPDATE MediaItems
    SET AverageScore = @Score
    WHERE Id = @MediaId;
END
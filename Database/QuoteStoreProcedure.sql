
-- Notable quotes for a media item, shown as a carousel in the details, ordered by SortOrder.
-- QuoteFrom is optional: who said it / where it's from (character, chapter, episode...).


-- GET -> GET ALL QUOTES BY MEDIA ID
CREATE OR ALTER PROCEDURE GetQuotesByMediaId
    @MediaId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON; -- Stops 'rows affected' messages from messing with EF Core

    SELECT Id, MediaId, QuoteText, QuoteFrom, SortOrder
    FROM MediaQuotes
    WHERE MediaId = @MediaId
    ORDER BY SortOrder, Id;
END
GO


-- POST -> ADD QUOTE (appended to the end). Returns the created quote.
CREATE OR ALTER PROCEDURE AddQuoteToMedia
    @MediaId UNIQUEIDENTIFIER,
    @QuoteText VARCHAR(MAX),
    @QuoteFrom VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @QuoteId UNIQUEIDENTIFIER = NEWID();

    BEGIN TRANSACTION;

    INSERT INTO MediaQuotes (Id, MediaId, QuoteText, QuoteFrom, SortOrder)
    SELECT @QuoteId, @MediaId, @QuoteText, @QuoteFrom, ISNULL(MAX(SortOrder) + 1, 0)
    FROM MediaQuotes WITH (UPDLOCK, HOLDLOCK)
    WHERE MediaId = @MediaId;

    COMMIT TRANSACTION;

    SELECT Id, MediaId, QuoteText, QuoteFrom, SortOrder
    FROM MediaQuotes
    WHERE Id = @QuoteId;
END
GO


-- PUT -> EDIT QUOTE. Returns the quote (empty if it doesn't exist).
CREATE OR ALTER PROCEDURE UpdateQuoteById
    @QuoteId UNIQUEIDENTIFIER,
    @QuoteText VARCHAR(MAX),
    @QuoteFrom VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE MediaQuotes
    SET QuoteText = @QuoteText,
        QuoteFrom = @QuoteFrom
    WHERE Id = @QuoteId;

    SELECT Id, MediaId, QuoteText, QuoteFrom, SortOrder
    FROM MediaQuotes
    WHERE Id = @QuoteId;
END
GO


-- PUT -> REORDER QUOTES
-- @QuoteIds is a JSON array of quote ids in the new order. Ids that don't belong
-- to @MediaId are ignored. Returns the quotes in their new order.
CREATE OR ALTER PROCEDURE ReorderMediaQuotes
    @MediaId UNIQUEIDENTIFIER,
    @QuoteIds NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE MediaQuotes
    SET SortOrder = CAST(j.[key] AS INT)
    FROM MediaQuotes
    INNER JOIN OPENJSON(@QuoteIds) j ON MediaQuotes.Id = TRY_CAST(j.value AS UNIQUEIDENTIFIER)
    WHERE MediaQuotes.MediaId = @MediaId;

    EXEC GetQuotesByMediaId @MediaId;
END
GO


-- DELETE -> DELETE QUOTE. Returns its media id (empty if it didn't exist).
CREATE OR ALTER PROCEDURE DeleteQuoteById
    @QuoteId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM MediaQuotes
    OUTPUT deleted.MediaId
    WHERE Id = @QuoteId;
END
GO


-- Statuses (Planning, Consuming...). MediaItems.Status stores the status *name*,
-- so renaming a status renames it on every item too.
-- Status rows returned to the app: Id, Name, SortOrder, MediaCount (items not deleted)


-- GET -> GET ALL STATUSES (in display order)
CREATE OR ALTER PROCEDURE GetAllStatuses
AS
BEGIN
    SET NOCOUNT ON;

    SELECT s.Id, s.Name, s.SortOrder,
        (SELECT COUNT(*) FROM MediaItems m WHERE m.Status = s.Name AND ISNULL(m.Deleted, 0) = 0) AS MediaCount
    FROM MediaStatuses s
    ORDER BY s.SortOrder, s.Id;
END
GO


-- POST -> ADD STATUS (at the end). Returns the new status.
CREATE OR ALTER PROCEDURE AddStatus
    @Name VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    INSERT INTO MediaStatuses (Name, SortOrder)
    SELECT @Name, ISNULL(MAX(SortOrder) + 1, 0)
    FROM MediaStatuses WITH (UPDLOCK, HOLDLOCK);

    DECLARE @Id INT = SCOPE_IDENTITY();

    COMMIT TRANSACTION;

    SELECT Id, Name, SortOrder, 0 AS MediaCount
    FROM MediaStatuses
    WHERE Id = @Id;
END
GO


-- PUT -> RENAME STATUS. Items with the old name get the new one, in the same transaction.
-- Returns the status (empty if it doesn't exist).
CREATE OR ALTER PROCEDURE UpdateStatusById
    @StatusId INT,
    @Name VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @OldName VARCHAR(50) = (SELECT Name FROM MediaStatuses WHERE Id = @StatusId);

    IF @OldName IS NOT NULL
    BEGIN
        BEGIN TRANSACTION;

        UPDATE MediaStatuses SET Name = @Name WHERE Id = @StatusId;
        UPDATE MediaItems SET Status = @Name WHERE Status = @OldName;

        COMMIT TRANSACTION;
    END

    SELECT s.Id, s.Name, s.SortOrder,
        (SELECT COUNT(*) FROM MediaItems m WHERE m.Status = s.Name AND ISNULL(m.Deleted, 0) = 0) AS MediaCount
    FROM MediaStatuses s
    WHERE s.Id = @StatusId;
END
GO


-- DELETE -> DELETE STATUS
-- A status that items still use can't simply be deleted: pass @ReassignTo (another status
-- name) to move those items first. Returns Deleted (1/0) and ItemCount.
-- No row if the status doesn't exist.
CREATE OR ALTER PROCEDURE DeleteStatusById
    @StatusId INT,
    @ReassignTo VARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Name VARCHAR(50) = (SELECT Name FROM MediaStatuses WHERE Id = @StatusId);

    IF @Name IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS Deleted, 0 AS ItemCount WHERE 1 = 0; -- empty result
        RETURN;
    END

    -- Only items that aren't deleted block the delete (same count Settings shows)
    DECLARE @ItemCount INT = (SELECT COUNT(*) FROM MediaItems WHERE Status = @Name AND ISNULL(Deleted, 0) = 0);

    IF @ItemCount > 0 AND @ReassignTo IS NULL
    BEGIN
        SELECT CAST(0 AS BIT) AS Deleted, @ItemCount AS ItemCount;
        RETURN;
    END

    BEGIN TRANSACTION;

    IF @ReassignTo IS NOT NULL
        UPDATE MediaItems SET Status = @ReassignTo WHERE Status = @Name;

    DELETE FROM MediaStatuses WHERE Id = @StatusId;

    COMMIT TRANSACTION;

    SELECT CAST(1 AS BIT) AS Deleted, @ItemCount AS ItemCount;
END
GO

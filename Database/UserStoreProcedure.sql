-- Users sign in with Google (see AuthController). Run after Schema.sql.


-- POST -> SIGN IN: create the user on first sign-in, refresh email/picture after that.
-- The display name is only set on creation so a later rename isn't overwritten.
CREATE OR ALTER PROCEDURE UpsertGoogleUser
    @GoogleSub VARCHAR(255),
    @Email NVARCHAR(320),
    @DisplayName NVARCHAR(100),
    @PictureUrl VARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM Users WHERE GoogleSub = @GoogleSub)
        UPDATE Users
        SET Email = @Email, PictureUrl = @PictureUrl, LastSignIn = GETDATE()
        WHERE GoogleSub = @GoogleSub;
    ELSE
        INSERT INTO Users (GoogleSub, Email, DisplayName, PictureUrl, LastSignIn)
        VALUES (@GoogleSub, @Email, @DisplayName, @PictureUrl, GETDATE());

    SELECT Id, Email, DisplayName, PictureUrl FROM Users WHERE GoogleSub = @GoogleSub;
END
GO


-- GET -> ALL USERS (to show names/avatars next to items and reviews)
CREATE OR ALTER PROCEDURE GetAllUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Email, DisplayName, PictureUrl FROM Users ORDER BY DisplayName;
END
GO


-- GET -> ONE USER
CREATE OR ALTER PROCEDURE GetUserById
    @UserId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;
    SELECT Id, Email, DisplayName, PictureUrl FROM Users WHERE Id = @UserId;
END
GO

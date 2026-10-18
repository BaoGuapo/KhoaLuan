-- Compatibility repair for databases created using a different migration history.
-- Matches RequirePasswordChangeOnFirstLogin without changing migration records.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 50001, 'Users table is missing. Apply the identity schema first.', 1;

IF COL_LENGTH(N'dbo.Users', N'MustChangePassword') IS NULL
    ALTER TABLE [dbo].[Users] ADD [MustChangePassword] bit NOT NULL
        CONSTRAINT [DF_Users_MustChangePassword] DEFAULT (0) WITH VALUES;

COMMIT TRANSACTION;

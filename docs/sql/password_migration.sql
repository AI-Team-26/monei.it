-- Password Hash Migration Script
-- Bug 16: migrate Account.Password from plain text to Argon2id hashes.
-- The app stores a self-describing PHC string ($argon2id$v=19$m=...,t=...,p=...$salt$hash),
-- at most 97 chars today; NVARCHAR(128) leaves room for longer parameter strings.
-- A NULL password means "not set / needs reset"; VerifyPassword fails closed on it.
-- Run this script against the SQL Server database before deploying the updated application.
-- Take a database backup first: blanking the passwords has no undo (the rollback below cannot
-- restore them either).

SET XACT_ABORT ON;
BEGIN TRAN;

-- 1. Widen the Password column (was NVARCHAR(50)). It stays nullable: NULL is how the app
--    represents an unset password.
--    COL_LENGTH reports BYTES, so nvarchar(128) means 256; comparing against 128 would skip the
--    ALTER for any column between nvarchar(64) and nvarchar(127).
IF COL_LENGTH('Account', 'Password') < 256
    ALTER TABLE [Account] ALTER COLUMN [Password] NVARCHAR(128) NULL;

-- 2. Existing values are plain text and cannot be re-hashed in place. Set them to NULL so no
--    plain text stays at rest. Login stays blocked until each user resets their password.
--    Already-migrated rows hold a $argon2id$... string and are left untouched, which keeps
--    this script safe to run more than once.
UPDATE [Account]
SET [Password] = NULL
WHERE [Password] NOT LIKE '$argon2id$%';

-- 3. Reset flag: DEFAULT 0 keeps new rows clean. A NULL password always means "needs reset"
--    (SetPassword rejects empty input), so mark exactly those rows on every run - this
--    also catches legacy rows written during the migration window after a first run.
IF COL_LENGTH('Account', 'PasswordResetRequired') IS NULL
    ALTER TABLE [Account] ADD [PasswordResetRequired] BIT NOT NULL CONSTRAINT DF_Account_PasswordResetRequired DEFAULT 0;

UPDATE [Account]
SET [PasswordResetRequired] = 1
WHERE [Password] IS NULL;

-- 4. Self-verification inside the same transaction: a wrong outcome rolls back everything,
--    leaving the database exactly as it was. Wider-than-nvarchar(128) is fine (same rule as
--    step 1). THROW, not RAISERROR: it terminates the batch, so no runner sees success after
--    a failed check even without sqlcmd -b.
IF COL_LENGTH('Account', 'Password') < 256
   OR COL_LENGTH('Account', 'PasswordResetRequired') IS NULL
   OR NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_NAME = 'Account' AND COLUMN_NAME = 'Password' AND IS_NULLABLE = 'YES')
BEGIN
    ROLLBACK;
    THROW 50000, N'Password migration failed verification: Account.Password must be at least nvarchar(128) nullable and Account.PasswordResetRequired must exist.', 1;
END

COMMIT;

-- 5. Rollback (if needed). Steps 1-4 are one transaction, so a failure already rolled back;
--    use this only to undo a successful run. Blanked passwords cannot be restored.
--    The shrink to NVARCHAR(50) fails while any 97-char hash is stored: blank the Password
--    column first (set it to NULL), which locks every user out again until they reset their
--    password.
--    The default constraint must go before the column (SQL Server error 5074 otherwise).
-- ALTER TABLE [Account] DROP CONSTRAINT DF_Account_PasswordResetRequired;
-- ALTER TABLE [Account] DROP COLUMN [PasswordResetRequired];
-- ALTER TABLE [Account] ALTER COLUMN [Password] NVARCHAR(50) NULL;

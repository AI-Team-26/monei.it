-- Password Hash Migration Script
-- Bug 16: migrate Account.Password from plain text to Argon2id hashes.
-- The app stores a self-describing PHC string ($argon2id$v=19$m=...,t=...,p=...$salt$hash),
-- at most 97 chars today; NVARCHAR(128) leaves room for longer parameter strings.
-- Run this script against the SQL Server database before deploying the updated application.

-- 1. Widen the Password column (was NVARCHAR(50)).
IF COL_LENGTH('Account', 'Password') < 128
    ALTER TABLE [Account] ALTER COLUMN [Password] NVARCHAR(128) NOT NULL;

-- 2. Existing values are plain text and cannot be re-hashed in place.
--    Blank them so no plain text stays at rest. VerifyPassword rejects an empty stored value,
--    so login stays blocked until each user resets their password.
--    Already-migrated rows hold a $argon2id$... string and are left untouched, which keeps
--    this script safe to run more than once.
UPDATE [Account]
SET [Password] = ''
WHERE [Password] <> '' AND [Password] NOT LIKE '$argon2id$%';

-- 3. Reset flag: DEFAULT 0 keeps new rows clean; existing rows are marked as needing reset.
IF COL_LENGTH('Account', 'PasswordResetRequired') IS NULL
BEGIN
    ALTER TABLE [Account] ADD [PasswordResetRequired] BIT NOT NULL DEFAULT 0;
    UPDATE [Account] SET [PasswordResetRequired] = 1;
END

-- 4. Verification: check column sizes
SELECT 
    TABLE_NAME, 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Account' AND COLUMN_NAME IN ('Password', 'PasswordResetRequired');

-- Expected result: Password = nvarchar(128), PasswordResetRequired = bit

-- 5. Rollback (if needed). Blanked passwords cannot be restored.
-- ALTER TABLE [Account] DROP COLUMN [PasswordResetRequired];
-- ALTER TABLE [Account] ALTER COLUMN [Password] NVARCHAR(50) NOT NULL;

-- Password Hash Migration Script
-- Bug 16: Migrate Account.Password from plain text to PBKDF2 hash
-- Run this script against the SQL Server database before deploying the updated application.

-- 1. Widen the Password column to accommodate PBKDF2 hash (Base64-encoded salt+hash = ~88 chars)
ALTER TABLE [Account]
ALTER COLUMN [Password] NVARCHAR(128) NOT NULL;

-- 2. Migration strategy for existing plain text passwords:
--    Since we cannot verify if existing values are plain text or already hashed,
--    the safest approach is to mark all existing passwords as invalid and force password reset.
--    
--    Option A: Force password reset for all users (RECOMMENDED for security)
--    Set a flag or marker that triggers password reset on next login.
--    This requires application-level support.

--    Option B: Attempt to re-hash (NOT RECOMMENDED - can't verify if already hashed)
--    Only safe if you KNOW all current values are plain text and no previous migration ran.

-- 3. For production: Create a password reset workflow
--    - Add a PasswordResetRequired BIT column (default 1 for existing users, 0 for new)
--    - On login, if PasswordResetRequired = 1, redirect to password reset flow
--    - After reset, set PasswordResetRequired = 0

-- Example: Add reset flag column
ALTER TABLE [Account]
ADD [PasswordResetRequired] BIT NOT NULL DEFAULT 1;

-- Update existing rows to require reset
UPDATE [Account]
SET [PasswordResetRequired] = 1;

-- For new users, the application should set PasswordResetRequired = 0 after first password set

-- 4. Verification: Check column size
SELECT 
    TABLE_NAME, 
    COLUMN_NAME, 
    DATA_TYPE, 
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Account' AND COLUMN_NAME = 'Password';

-- Expected result: CHARACTER_MAXIMUM_LENGTH = 128

-- 5. Rollback (if needed)
-- ALTER TABLE [Account] ALTER COLUMN [Password] NVARCHAR(50) NOT NULL;
-- ALTER TABLE [Account] DROP COLUMN [PasswordResetRequired];
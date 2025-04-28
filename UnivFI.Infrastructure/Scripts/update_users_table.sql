-- Users 테이블에 리프레시 토큰 관련 열 추가
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns 
    WHERE name = 'RefreshToken'
    AND object_id = OBJECT_ID('Users')
)
BEGIN
    ALTER TABLE Users
    ADD RefreshToken NVARCHAR(MAX) NULL;
END

IF NOT EXISTS (
    SELECT 1
    FROM sys.columns 
    WHERE name = 'RefreshTokenExpiry'
    AND object_id = OBJECT_ID('Users')
)
BEGIN
    ALTER TABLE Users
    ADD RefreshTokenExpiry DATETIME2 NULL;
END

-- 인덱스 추가 (리프레시 토큰으로 빠르게 조회할 수 있도록)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes 
    WHERE name = 'IX_Users_RefreshToken' 
    AND object_id = OBJECT_ID('Users')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_Users_RefreshToken
    ON Users(RefreshToken)
    WHERE RefreshToken IS NOT NULL;
END

-- 인덱스 추가 (UserName으로 빠르게 조회할 수 있도록)
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes 
    WHERE name = 'IX_Users_UserName' 
    AND object_id = OBJECT_ID('Users')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX IX_Users_UserName
    ON Users(UserName)
    WHERE UserName IS NOT NULL;
END 
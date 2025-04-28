CREATE TABLE [dbo].[Users] (
    [Id]                 INT            IDENTITY (1, 1) NOT NULL,
    [UserName]           NVARCHAR (100) NOT NULL,
    [Password]           NVARCHAR (128) NOT NULL,
    [Salt]               NVARCHAR (100) NOT NULL,
    [RefreshToken]       NVARCHAR (200) NULL,
    [RefreshTokenExpiry] DATETIME       NULL,
    [Name]               NVARCHAR (50)  NULL,
    [Email]              VARCHAR (80)   NULL,
    [CreatedAt]          DATETIME2 (7)  NULL,
    [UpdatedAt]          DATETIME2 (7)  NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_Users_RefreshToken]
    ON [dbo].[Users]([RefreshToken] ASC) WHERE ([RefreshToken] IS NOT NULL);


GO
CREATE NONCLUSTERED INDEX [IX_Users_UserName]
    ON [dbo].[Users]([UserName] ASC);


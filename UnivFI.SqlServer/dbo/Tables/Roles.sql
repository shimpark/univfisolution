CREATE TABLE [dbo].[Roles] (
    [Id]          INT             IDENTITY (1, 1) NOT NULL,
    [RoleName]    NVARCHAR (100)  NOT NULL,
    [RoleComment] NVARCHAR (1000) NULL,
    [CreatedAt]   DATETIME2 (7)   NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);


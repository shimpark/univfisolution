CREATE TABLE [dbo].[Menus] (
    [Id]         INT            IDENTITY (1, 1) NOT NULL,
    [MenuKey]    NVARCHAR (100) NOT NULL,
    [Url]        NVARCHAR (200) NOT NULL,
    [Title]      NVARCHAR (200) NULL,
    [ParentId]   INT            NULL,
    [MenuOrder]  SMALLINT       NULL,
    [Levels]     SMALLINT       NULL,
    [UseNewIcon] BIT            NULL,
    [CreatedAt]  DATETIME2 (7)  NULL,
    [UpdatedAt]  DATETIME2 (7)  NULL,
    CONSTRAINT [PK__Menus__3214EC07C8930A80] PRIMARY KEY CLUSTERED ([Id] ASC)
);


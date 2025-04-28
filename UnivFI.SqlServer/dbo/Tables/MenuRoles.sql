CREATE TABLE [dbo].[MenuRoles] (
    [MenuId] INT NOT NULL,
    [RoleId] INT NOT NULL,
    PRIMARY KEY CLUSTERED ([MenuId] ASC, [RoleId] ASC),
    CONSTRAINT [FK_MenuRoles_Menus] FOREIGN KEY ([MenuId]) REFERENCES [dbo].[Menus] ([Id]),
    CONSTRAINT [FK_MenuRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles] ([Id])
);


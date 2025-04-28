CREATE TABLE [dbo].[FileAttaches] (
    [FileAttachId] BIGINT          IDENTITY (1, 1) NOT NULL,
    [FilePath]     NVARCHAR (1000) NULL,
    [FileName]     NVARCHAR (500)  NULL,
    [FileType]     NVARCHAR (10)   NULL,
    [FileLength]   BIGINT          NULL,
    [FileGUID]     VARCHAR (200)   NULL,
    [CreatedAt]    DATETIME2 (7)   NULL, 
    CONSTRAINT [PK_FileAttaches] PRIMARY KEY ([FileAttachId])
);


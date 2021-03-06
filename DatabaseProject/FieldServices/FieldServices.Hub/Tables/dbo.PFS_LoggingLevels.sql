USE [Hub]
GO

--drop table PFS_LoggingLevels

CREATE TABLE [dbo].[PFS_LoggingLevels](
	[Id] INT  NOT NULL UNIQUE,
	[Name] [varchar](50) NOT NULL
PRIMARY KEY CLUSTERED 
([Id]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] 
GO
INSERT INTO [dbo].[PFS_LoggingLevels] ([Id],[Name]) VALUES (1,'FATAL')
INSERT INTO [dbo].[PFS_LoggingLevels] ([Id],[Name]) VALUES (2,'ERROR')
INSERT INTO [dbo].[PFS_LoggingLevels] ([Id],[Name]) VALUES (3,'WARN')
INSERT INTO [dbo].[PFS_LoggingLevels] ([Id],[Name]) VALUES (4,'INFO')
INSERT INTO [dbo].[PFS_LoggingLevels] ([Id],[Name]) VALUES (5,'DEBUG')
INSERT INTO [dbo].[PFS_LoggingLevels] ([Id],[Name]) VALUES (6,'TRACE')
GO
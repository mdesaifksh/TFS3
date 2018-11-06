USE [Hub]
GO

--drop table LoggingLevels

CREATE TABLE [dbo].[LoggingLevels](
	[Id] INT  NOT NULL UNIQUE,
	[Name] [varchar](50) NOT NULL
PRIMARY KEY CLUSTERED 
([Id]) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] 
GO
INSERT INTO [dbo].[LoggingLevels] ([Id],[Name]) VALUES (1,'Fatal')
INSERT INTO [dbo].[LoggingLevels] ([Id],[Name]) VALUES (2,'Error')
INSERT INTO [dbo].[LoggingLevels] ([Id],[Name]) VALUES (3,'Warn')
INSERT INTO [dbo].[LoggingLevels] ([Id],[Name]) VALUES (4,'Info')
INSERT INTO [dbo].[LoggingLevels] ([Id],[Name]) VALUES (5,'Debug')
INSERT INTO [dbo].[LoggingLevels] ([Id],[Name]) VALUES (6,'Trace')
GO
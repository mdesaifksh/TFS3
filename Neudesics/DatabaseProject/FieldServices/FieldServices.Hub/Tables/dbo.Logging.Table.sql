USE [Hub]
GO
--DROP TABLE Hub.DBO.Logging
CREATE TABLE [dbo].Logging(
	[LogDate] [datetime]  NOT NULL DEFAULT(GETDATE()),
	[AppName] [varchar](200) NOT NULL,
	[User] [varchar](100) NULL,
	[Level] [int] NOT null ,
	[MachineName] [varchar](100) NOT NULL,	
	[Message] [varchar](max) NOT NULL,
	[Exception] [varchar](max) NULL,
	[Details] [varchar](max) NULL,	
	FOREIGN KEY ([Level]) REFERENCES Hub.dbo.LoggingLevels(Id)  
) 
USE [Hub]
GO
--DROP TABLE Hub.DBO.PFS_Logging
CREATE TABLE [dbo].PFS_Logging(
	[LogDate]			[datetime]  NOT NULL DEFAULT(GETDATE()),
	[AppName]			[varchar](200) NOT NULL,
	[SprocName]			[varchar](200) NOT NULL,
	[User]				[varchar](100) NULL,
	[Level]				[int] NOT null ,
	[MachineName]		[varchar](100) NOT NULL,	
	[Message]			[varchar](max) NOT NULL,
	[Exception]			[varchar](max) NULL,
	[DetailsJson]		[varchar](max) NULL,	
	FOREIGN KEY ([Level]) REFERENCES Hub.dbo.PFS_LoggingLevels(Id)  
) 

GO
CREATE CLUSTERED INDEX [ClusteredIndex-20181106-171953] ON [dbo].[PFS_Logging]
(
	[LogDate] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


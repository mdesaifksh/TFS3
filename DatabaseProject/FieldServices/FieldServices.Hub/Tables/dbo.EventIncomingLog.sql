USE [Hub]
GO

--drop  TABLE [dbo].[EventIncomingLog]

CREATE TABLE [dbo].[PFS_EventIncomingLog](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	Topic varchar(500) NOT NULL,
	Message_Subject varchar(500) NOT NULL,
	Message_Sent datetime not null,	
	[Json_Payload] [varchar](max) NULL,
	CreatedAt datetime not null default(getdate()),
	[RowVersion] [timestamp] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO


/*
ALTER TABLE PFS_EventIncomingLog
ADD [RowVersion] [timestamp] NOT NULL
*/
--select * from [EventIncomingLog]


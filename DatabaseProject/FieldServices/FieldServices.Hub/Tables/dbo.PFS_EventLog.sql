USE [Hub]
GO
/****** Object:  Table [dbo].[PFS_EventLog]    Script Date: 11/5/2018 6:18:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PFS_EventLog](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Event_ID] [int] NOT NULL,
	[Source_ID] [int] NULL,
	[Load_Date] [datetime] NOT NULL,
	[Voyager_Property_HMY] [int] NULL,
	[Json_Payload] [varchar](max) NULL,
	[RowVersion] [timestamp] NOT NULL,
	[Voyager_Property_SCode] varchar(50) null
	SFCode INT null
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

CREATE CLUSTERED INDEX [ClusteredIndex-20181219-153708] ON [dbo].[PFS_EventLog]
(
	[Load_Date] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


CREATE NONCLUSTERED INDEX [NonClusteredIndex-20181219-153752] ON [dbo].[PFS_EventLog]
(
	[Voyager_Property_HMY] ASC,
	[Event_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO;

--To Support Init Reno
ALTER TABLE [PFS_EventLog] ALTER COLUMN [Voyager_Property_HMY] INT NULL
ALTER TABLE [PFS_EventLog] ALTER COLUMN [Voyager_Property_SCode] varchar(50) null
ALTER TABLE [PFS_EventLog] Add SFCode INT NULL

--SET IDENTITY_INSERT [dbo].[PFS_EventLog] ON 

--INSERT [dbo].[PFS_EventLog] ([ID], [Event_ID], [Source_ID], [Load_Date], [Voyager_Property_HMY], [Json_Payload]) VALUES (1, 1, 1, CAST(N'2018-11-01T18:18:11.620' AS DateTime), 80521, N'{
--  "Date1": "11/5/2018",
--  "Event": "1"},
--  "IsForce": false,
--  "PropertyID": "80521"
--}')
--SET IDENTITY_INSERT [dbo].[PFS_EventLog] OFF
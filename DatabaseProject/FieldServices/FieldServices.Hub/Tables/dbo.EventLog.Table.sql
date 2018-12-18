USE [Hub]
GO
/****** Object:  Table [dbo].[EventLog]    Script Date: 11/5/2018 6:18:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EventLog](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Event_ID] [int] NOT NULL,
	[Source_ID] [int] NULL,
	[Load_Date] [datetime] NOT NULL,
	[Voyager_Property_HMY] [int] NOT NULL,
	[Json_Payload] [varchar](max) NULL,
	[RowVersion] [timestamp] NOT NULL,
	[Voyager_Property_SCode] varchar(50) not null
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
--SET IDENTITY_INSERT [dbo].[EventLog] ON 

--INSERT [dbo].[EventLog] ([ID], [Event_ID], [Source_ID], [Load_Date], [Voyager_Property_HMY], [Json_Payload]) VALUES (1, 1, 1, CAST(N'2018-11-01T18:18:11.620' AS DateTime), 80521, N'{
--  "Date1": "11/5/2018",
--  "Event": "1"},
--  "IsForce": false,
--  "PropertyID": "80521"
--}')
--SET IDENTITY_INSERT [dbo].[EventLog] OFF

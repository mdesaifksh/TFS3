
-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Tables\dbo.PFS_LoggingLevels.sql
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
-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Tables\dbo.PFS_Logging.sql
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


-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Tables\dbo.PFS_EventDefinition.sql
USE [Hub];
GO
/*
Create Event Definition Table
*/
CREATE TABLE [dbo].[PFS_EventDefinition](
	[Event_ID] [int] NOT NULL,
	[Event_Name] [varchar](100) NULL,
	[Event_Description] [varchar](255) NULL,
	[Event_Name_Old] [varchar](120) NULL	
) ON [PRIMARY];

GO

--Create Index
CREATE UNIQUE CLUSTERED INDEX [ClusteredIndex-20181217-113156] ON [dbo].[PFS_EventDefinition]
(
	[Event_ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]




GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Resident Notice to Move Out Received', N'Resident has been given notice to move out.', N'60 day notice sent to resident', 1)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Assign Project Manager ', N'Assign Project Manager', NULL, 2)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Corporate Renewals', N'Corporate renewals started.  When completed that means tenant has resigned lease and turnover has been cancelled.', N'Lease Renewal received', 3)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Market Schedules Pre-Move-Out', N'Yardi Pre-Move out appointment', N'Pre-move-out scheduled', 4)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Pre-Move-Out Inspection', N'Pre-Move-out inspection ', N'Move-out inspection completed', 5)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Move-Out Inspection', N'Move-Out inspection', N'Property Walked', 6)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Budget Start', N'Job Budget Started', N'Job Budget Started', 7)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Budget Approval', N'Budget is Approved', NULL, 8)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Job Assignment to Vendor(s) in Contract Creator', N'Contract Creator (Dynamics):  Job item assigned to vendor', N'Job item assigned to vendor', 9)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Job and Contract(s) Submitted to Yardi', N'Yardi: Job created in Yardi', N'Job Created in Yardi', 10)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Vendor(s) Says Job Started', N'Vender says job started', NULL, 11)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Work In Progress', N'FotoNotes:  Interim inspection completed', N'Interim inspection completed', 12)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Vendor Requests Change Order if Necessary', N'Vendor can request a change order.', NULL, 13)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Change Order Approved', N'Change Order Approved', NULL, 14)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Vender Says Job Complete', N'Vendor says job is completed', NULL, 15)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Quality Control Inspection', N'Work inspected before job completion', NULL, 16)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Job Completed', N'FotoNotes:  QC inspection completed indicating no additional work needed', N'QC inspection completed indicating no additional work needed', 17)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Hero Shot Picture', N'Work photographed', NULL, 18)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Marketing Inspection', N'FotoNotes:  Marketing â€œinspectionâ€ completed', N'Marketing â€œinspectionâ€ completed', 19)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Bi-weekly inspection', N'FotoNotes:  Bi-weekly inspection completed', N'Bi-weekly inspection completed', 20)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Move-in inspection completed', N'FotoNotes:  Move-in inspection completed', N'Move-in inspection completed', 21)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'30 day notice sent to resident', N'Move out notice sent < 30 days till.', N'30 day notice sent to resident', 103)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Vendor Job complete ', N'Yardi:  Job complete (Look into making this possible in Dynamics for MVP)', N'Vendor Job complete ', 110)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'QC inspection completed indicating additional work needed', N'QC inspection completed indicating additional work needed', N'QC inspection completed indicating additional work needed', 112)
GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Move Out Date Changed', N'When Move Out Date changes updates need to be made.', NULL, 1001)
GO


-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Tables\dbo.PFS_EventDefinition.RenoUpdate.sql
USE [Hub];
GO
--select * from hub.dbo.[PFS_EventDefinition]


/*
*	Now add Renowalk events and Ids
*/
ALTER TABLE Hub.dbo.[PFS_EventDefinition]
ADD Reno_ID int null

GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_ID], [Reno_ID]) 
VALUES (N'Offer Accepted', N'Offer accepted.', 201, 201)
GO

Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 202
where Event_ID =2

GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_ID], [Reno_ID]) 
VALUES (N'Schedule Due Diligence Inspection', N'Schedule Due Diligence Inspection.', 203, 203)
GO

--Budget Start
Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 204
where Event_ID =7

Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 205
where Event_ID =8

GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_ID], [Reno_ID]) 
VALUES (N'Offer Rejected or Approved', N'Offer Rejected or Approved.', 206, 206)
GO

Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 207
where Event_ID =9


GO
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_ID], [Reno_ID]) 
VALUES (N'Close Escrow', N'Close Escrow.', 208, 208)
GO

Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 209
where Event_ID =10

Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 210
where Event_ID =11
Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 211
where Event_ID =12
Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 212
where Event_ID =13
Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 213
where Event_ID =14

Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 214
where Event_ID =15
Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 215
where Event_ID =16
Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 216
where Event_ID =17

Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 217
where Event_ID =18
Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 218
where Event_ID =19
Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 219
where Event_ID =20
Update hub.dbo.[PFS_EventDefinition]
set Reno_ID = 220
where Event_ID =21

GO
--select * from hub.dbo.[PFS_EventDefinition] order by isnull(Reno_Id,999999), Event_ID


-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Tables\dbo.PFS_EventSource.sql

/****** Object:  Table [dbo].[PFS_EventSource]    Script Date: 11/5/2018 6:18:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PFS_EventSource](
	[Source_ID] [int] IDENTITY(1,1) NOT NULL,
	[Source_Name] [varchar](50) NULL
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[PFS_EventSource] ON 

INSERT [dbo].[PFS_EventSource] ([Source_ID], [Source_Name]) VALUES (1, N'Voyager')
INSERT [dbo].[PFS_EventSource] ([Source_ID], [Source_Name]) VALUES (2, N'Salesforce')
INSERT [dbo].[PFS_EventSource] ([Source_ID], [Source_Name]) VALUES (3, N'Renowalk')
INSERT [dbo].[PFS_EventSource] ([Source_ID], [Source_Name]) VALUES (4, N'Fotonotes')
INSERT [dbo].[PFS_EventSource] ([Source_ID], [Source_Name]) VALUES (5, N'RentCafe')
INSERT [dbo].[PFS_EventSource] ([Source_ID], [Source_Name]) VALUES (6, N'Fischer')
SET IDENTITY_INSERT [dbo].[PFS_EventSource] OFF

-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Tables\dbo.PFS_EventLog.sql
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
-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Tables\dbo.PFS_EventIncomingLog.sql
USE [Hub]
GO

--drop  TABLE [dbo].[PFS_EventIncomingLog]

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
--select * from [PFS_EventIncomingLog]


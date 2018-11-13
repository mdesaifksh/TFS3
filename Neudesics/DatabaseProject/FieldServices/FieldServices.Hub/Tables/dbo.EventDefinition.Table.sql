USE [Hub]
GO
/****** Object:  Table [dbo].[EventDefinition]    Script Date: 11/5/2018 6:18:53 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[EventDefinition](
	[Event_ID] [int] IDENTITY(1,1) NOT NULL,
	[Event_Name] [varchar](100) NULL,
	[Event_Description] [varchar](255) NULL
) ON [PRIMARY]
GO
SET IDENTITY_INSERT [dbo].[EventDefinition] ON 

INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (1, N'60 day notice sent to resident', N'Move out notice sent > 30 days till.')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (2, N'Lease Renewal received', N'Move out has been cancelled.')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (3, N'30 day notice sent to resident', N'Move out notice sent < 30 days till.')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (4, N'Pre-move-out scheduled', N'Yardi Pre-Move out appointment')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (5, N'Job Budget Started', N'Renowalk Job Budget Started')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (6, N'Move-out inspection completed', N'FotoNotes Move-out inspection completed')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (7, N'Property Walked', N'RenoWalk:  Project Status = Walked')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (8, N'Job Created in Yardi', N'Yardi: Job created in Yardi')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (9, N'Job item assigned to vendor', N'Contract Creator (Dynamics):  Job item assigned to vendor')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (10, N'Interim inspection completed', N'FotoNotes:  Interim inspection completed')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (11, N'Vendor Job complete ', N'Yardi:  Job complete (Look into making this possible in Dynamics for MVP)')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (12, N'QC inspection completed indicating additional work needed', N'FotoNotes:  QC inspection completed indicating additional work needed')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (13, N'QC inspection completed indicating no additional work needed', N'FotoNotes:  QC inspection completed indicating no additional work needed')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (14, N'Marketing “inspection” completed', N'FotoNotes:  Marketing “inspection” completed')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (15, N'Bi-weekly inspection completed', N'FotoNotes:  Bi-weekly inspection completed')
INSERT [dbo].[EventDefinition] ([Event_ID], [Event_Name], [Event_Description]) VALUES (16, N'Move-in inspection completed', N'FotoNotes:  Move-in inspection completed')
SET IDENTITY_INSERT [dbo].[EventDefinition] OFF

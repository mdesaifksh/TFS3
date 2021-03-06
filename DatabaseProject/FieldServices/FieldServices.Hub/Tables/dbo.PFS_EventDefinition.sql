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
INSERT [dbo].[PFS_EventDefinition] ([Event_Name], [Event_Description], [Event_Name_Old], [Event_ID]) VALUES (N'Marketing Inspection', N'FotoNotes:  Marketing “inspection” completed', N'Marketing “inspection” completed', 19)
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


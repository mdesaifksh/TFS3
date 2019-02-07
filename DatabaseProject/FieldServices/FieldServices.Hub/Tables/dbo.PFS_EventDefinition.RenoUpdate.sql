USE [Hub];
GO
--select * from hub.dbo.[PFS_EventDefinition]

--begin tran

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

select * from hub.dbo.[PFS_EventDefinition] order by isnull(Reno_Id,999999), Event_ID

--rollback tran 
USE [HUB]
-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_LogMessage.sql

GO

Create or Alter PROCEDURE dbo.PFS_LogMessage
	 @LogLevel AS Int
	,@SprocName as VARCHAR(200)
	,@Message AS VARCHAR(Max)
	,@AppName as VARCHAR(200) = 'Hub'
	,@Exception AS VARCHAR(Max) = null
	,@DetailsJson AS VARCHAR(MAX) = null
	,@User AS VARCHAR(100) = null
	,@MachineName AS VARCHAR(100) = null
	,@LogDate As Datetime = null
AS

BEGIN 

SET @LogDate	= COALESCE(@LogDate,GETDATE())
SET @User		= COALESCE(@User,SUSER_NAME())
SET @MachineName = COALESCE(@MachineName, @@SERVERNAME)

--Make sure Log Level passed in exists in LoggingLevels, otherwise default it to 4 (Info)
select @LogLevel = ll.Id
from hub.dbo.LoggingLevels ll
where ll.Id = @LogLevel

set @LogLevel = COALESCE(@LogLevel, 4)

INSERT INTO [dbo].[PFS_Logging]
           ([LogDate]
           ,[AppName]
           ,[SprocName]
           ,[User]
           ,[Level]
           ,[MachineName]
           ,[Message]
           ,[Exception]
           ,[DetailsJson])
     VALUES
           (@LogDate
           ,@AppName
           ,@SprocName
           ,@User
           ,@LogLevel
           ,@MachineName
           ,@Message
           ,@Exception
           ,@DetailsJson)

END
GO


-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_LogError.sql

GO

Create or Alter PROCEDURE dbo.PFS_LogError
	 @LogLevel AS Int
	,@SprocName as VARCHAR(200)
	,@Message AS VARCHAR(Max)
	,@AppName as VARCHAR(200) = 'Hub'	
	,@DetailsJson AS VARCHAR(MAX) = null
	,@User AS VARCHAR(100) = null
	,@MachineName AS VARCHAR(100) = null
	,@LogDate As Datetime = null
AS

BEGIN 
 
DECLARE @ErrorNumber INT = ERROR_NUMBER();
DECLARE @ErrorLine INT = ERROR_LINE();
DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
DECLARE @ErrorState INT = ERROR_STATE();

Declare @Exception Varchar(5000)
Set @Exception = 
	'Number:' + Convert(varchar(200), @ErrorNumber) 
	+ '; Line:' + Convert(varchar(200), @ErrorLine)
	+ '; Severity:' + Convert(varchar(200), @ErrorSeverity)
	+ '; State:' + Convert(varchar(200), @ErrorState)
	+ '; Message:' + @ErrorMessage;

exec PFS_LogMessage @LogLevel, @SprocName, @Message, @AppName, @Exception, @DetailsJson, @User, @MachineName, @LogDate

END
-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_MoveOut_Changed.sql

GO

--/******************************************************************************************* 
--Resident Notice to Move Out - Field Services Event Fire
--Description:	This procedure will write a record to the Hub.dbo.PFS_eventlog table when a property's
--				unit status changes to the designated status.
--*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_MoveOut_Changed
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'PFS_MoveOut_Changed'
exec Hub.dbo.PFS_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking for move out date changed.'

Declare @CreateEventId int = 1;
Declare @EventId int = 1001;
Declare @SourceId int = 1;

;with movedate_events as
(
	SELECT 		
		*		
		,ROW_NUMBER() OVER (PARTITION BY el.Voyager_Property_HMY  ORDER BY el.load_date desc) AS RID
    FROM   hub.dbo.PFS_eventlog el
	WHERE Event_ID = @EventId	
)
,movedate_last as (
	Select 
		Voyager_Property_HMY
		,Json_Payload
		,iif(isnull(ISJSON (json_payload),0) = 1, convert(date, JSON_VALUE(Json_Payload, '$.Date1')), null) as PayloadDate1   --Move out date doesn't need to consider time, so convert to date
	from	movedate_events
	WHERE	RID = 1
)
,tenants as
(
	SELECT 		
		*		
		,ROW_NUMBER() OVER (PARTITION BY t.HPROPERTY ORDER BY t.DTMOVEOUT desc, t.DTLEASETO desc) AS RID
    FROM   Homes.dbo.vYardi_Tenant t	
	WHERE DTMOVEIN <= getdate()
)
, yardi_data as (
select 
top 3  --For Testing
	t.DTMOVEOUT as MoveOutDate
	,movd.PayloadDate1 as PrevMoveOutDate
	,yd.YardiId
	,yd.YardiScode	
from 
Homes.dbo.vYardi_PropertyLevel_YD yd
LEFT JOIN Hub.dbo.view_PFS_EventStatus re on re.Voyager_Property_HMY = yd.YardiId
LEFT JOIN movedate_last movd on movd.Voyager_Property_HMY = yd.YardiId
LEFT JOIN tenants t on t.HPROPERTY = yd.YardiId and t.RID = 1
where 
	movd.PayloadDate1 is not null and
	re.Created = 1
	AND (
			(t.DTMOVEOUT is not null and (movd.PayloadDate1 is null OR convert(date,movd.PayloadDate1) <> convert(date,t.DTMOVEOUT) ))  --make sure they are compared on date
			OR (t.DTMOVEOUT is null and movd.PayloadDate1 is not null)
		)
)
   INSERT INTO hub.dbo.PFS_eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   voyager_property_hmy, 
				   voyager_property_scode,
                   json_payload) 
      SELECT @EventID                          AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date, 
             YardiId, 
			 YardiScode,			 
			 (select 
				Homes.dbo.fncs_ConvertESTtoUTC(MoveOutDate) as Date1
				,@EventID as [Event]
				,YardiScode  as PropertyID
				,CAST(0 as BIT) as IsForce
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   yardi_data 
		
declare @message varchar(200) = 'Finished Inserting Move Out Changed into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO

-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_Renowalk_Status.sql

GO


--/******************************************************************************************* 
--Description:	This procedure will write a record to the Hub.dbo.PFS_eventlog table when a property's
--				has a new yardi contract\job and has been sent to Dynamics.
--*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_Renowalk_Status
AS
BEGIN

SET NOCOUNT ON;

declare @SprocName varchar(100) = 'PFS_Renowalk_Status'
Declare @SourceId int = 1;

Declare @BudgetApprovedEventIdTurn int = 8;
Declare @BudgetApprovedEventIdReno int = 205;

Declare @BudgetStartedEventIdTurn int = 7;
Declare @BudgetStartedEventIdReno int = 204;	

/********************************************************************************
* Budget Started
********************************************************************************/

exec Hub.dbo.PFS_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking for started budgets and then approved.';

with rw_currstat as (  --Get current renowalk status
	select 
		row_number() over (partition by RenowalkID order by PropertyStatusCreateDate desc) as rid
		,PropertyStatusCreateDate
		,RenowalkID
		,PropertyStatus
	from hub.[dbo].[RNW_PropertyStatusHistory]	
)
,recent_event_job as (	--Get most recent budget started events
	SELECT 
		voyager_property_hmy 
		,max(load_date) as LastEventDate		
    FROM   hub.dbo.PFS_eventlog 
	WHERE Event_ID = @BudgetStartedEventIdTurn OR Event_ID = @BudgetStartedEventIdReno
	GROUP BY Voyager_Property_HMY
	HAVING max(load_date) >= convert(date, dateadd(day, -120, getdate()))
)
,updatedata as (  --Join relevant datasources and filter for new started budgets
	select 
		rw.PropertyStatus
		,p.YardihMy
		,p.YardiCode as YardiScode
		,CASE WHEN ce.[ProjectType] = 'Turn' THEN @BudgetStartedEventIdTurn	--set publish id based on project type.
			WHEN ce.[ProjectType] = 'Reno' THEN @BudgetStartedEventIdReno		
		ELSE @BudgetApprovedEventIdTurn END as PublishEventId
		,rej.Voyager_Property_HMY
	from 
		rw_currstat rw		
		inner join
		HUB.dbo.Property p on rw.RenowalkID = p.RenoWalkID AND rw.rid = 1
		INNER JOIN Hub.dbo.view_PFS_EventStatus ce on ce.Voyager_Property_HMY = p.YardihMy
		left join recent_event_job rej on rej.Voyager_Property_HMY = p.YardihMy
	WHERE
		rw.PropertyStatus = 'Walked'
		AND rej.Voyager_Property_HMY is null
		and ce.Created = 1
)
   INSERT INTO hub.dbo.PFS_eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   voyager_property_hmy, 
				   voyager_property_scode,
                   json_payload) 
      SELECT PublishEventId        AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date, 
             YardihMy, 
			 YardiScode,
			  (select 
				GETUTCDATE() as Date1
				,PublishEventId as [Event]
				,YardiScode  as PropertyID
				,CAST(0 as BIT) as IsForce				
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   updatedata 
		
declare @message varchar(200) = 'Finished Inserting Budget Started events into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;


/**********************************************************************************************
* Budget Approved
***********************************************************************************************/

with rw_currstat as (  --Get current renowalk status
	select 
		row_number() over (partition by RenowalkID order by PropertyStatusCreateDate desc) as rid
		,PropertyStatusCreateDate
		,RenowalkID
		,PropertyStatus
	from hub.[dbo].[RNW_PropertyStatusHistory]	
)
,recent_event_job as (	--Get most recent budget approved events
	SELECT 
		voyager_property_hmy 
		,max(load_date) as LastEventDate		
    FROM   hub.dbo.PFS_eventlog 
	WHERE Event_ID = @BudgetApprovedEventIdTurn OR Event_ID = @BudgetApprovedEventIdReno
	GROUP BY Voyager_Property_HMY
	HAVING max(load_date) >= convert(date, dateadd(day, -120, getdate()))
)
,updatedata as (  --Join relevant datasources and filter for new reviewed budgets
	select 
		rw.PropertyStatus
		,p.YardihMy
		,p.YardiCode as YardiScode
		,CASE WHEN ce.[ProjectType] = 'Turn' THEN @BudgetApprovedEventIdTurn	--set publish id based on project type.
			WHEN ce.[ProjectType] = 'Reno' THEN @BudgetApprovedEventIdReno		
		ELSE @BudgetApprovedEventIdTurn END as PublishEventId
		,rej.Voyager_Property_HMY
	from 
		rw_currstat rw		
		inner join
		HUB.dbo.Property p on rw.RenowalkID = p.RenoWalkID AND rw.rid = 1
		INNER JOIN Hub.dbo.view_PFS_EventStatus ce on ce.Voyager_Property_HMY = p.YardihMy
		left join recent_event_job rej on rej.Voyager_Property_HMY = p.YardihMy
	WHERE
		rw.PropertyStatus = 'Reviewed'
		AND rej.Voyager_Property_HMY is null
		and ce.Created = 1
)
   INSERT INTO hub.dbo.PFS_eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   voyager_property_hmy, 
				   voyager_property_scode,
                   json_payload) 
      SELECT PublishEventId					   AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date, 
             YardihMy, 
			 YardiScode,
			  (select 
				GETUTCDATE() as Date1
				,PublishEventId as [Event]
				,YardiScode  as PropertyID
				,CAST(0 as BIT) as IsForce				
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   updatedata 
		
declare @message2 varchar(200) = 'Finished Inserting Budget Approved events into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message2;

END
GO


-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_SFStatus_Acquired.sql

GO


/******************************************************************************************* 
Pre Acquisition Property Marked Exclude or no longer Exists - Field Services Event Fire
Description:	Checks Hub.dbo.Property for Excluded properties and current projects where the property has been removed.
*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_SFStatus_OfferRejected
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'PFS_SFStatus_Acquired'
exec Hub.dbo.PFS_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking Pre Acq Status for Acquired'

Declare @EventId int = 208;
Declare @SourceId int = 1;

with  SF_data as (
	select
	 top 3 
		p.YardihMy
		,p.YardiCode
	from 
	 Hub.dbo.view_PFS_EventStatus re
	 LEFT JOIN hub.dbo.property p  on re.SFCode = p.SFCode
	where 
		re.ProjectType = 'Reno' AND re.SFCode is not null
		AND sPreAcqStatus = 'Acquired'  --Excluded or doesn't exist
		AND isnull(re.Created, 0) = 1 --Project status is created
		AND p.YardihMy is not null  --Only mark acquired after Hub has yardi info
		and p.YardiCode is not null
)
   INSERT INTO hub.dbo.PFS_eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   [Voyager_Property_HMY], 
				   [Voyager_Property_SCode],
                   json_payload) 
      SELECT @EventID                          AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date,              
			 YardihMy							AS [Voyager_Property_HMY],
			 YardiCode							   AS [Voyager_Property_SCode],
			 (select 
				 getutcdate() as Date1
				,@EventID as [Event]
				,YardiCode  as PropertyID
				,CAST(0 as BIT) as IsForce
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   SF_data 
		
		
declare @message varchar(200) = 'Finished Inserting Records into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO



-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_SFStatus_OfferAccepted.sql

GO


/******************************************************************************************* 
Pre Acquisition Property Marked Committed - Field Services Event Fire
Description:	Checks Hub.dbo.Property for committed properties not previously sent and writes to event log.
*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_SFStatus_OfferAccepted
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'PFS_SFStatus_OfferAccepted'
exec Hub.dbo.PFS_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking Pre Acq Status for Committed'

Declare @EventId int = 201;
Declare @SourceId int = 1;

with  SF_data as (
	select
	 top 3 
	p.SFCode 
	from hub.dbo.property p
	LEFT JOIN Hub.dbo.view_PFS_EventStatus re on re.SFCode = p.SFCode
	where sPreAcqStatus = 'Committed' and isnull(re.Created, 0) = 0
	and sAcqType not like '%bulk%'
)
   INSERT INTO hub.dbo.PFS_eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   SFCode, 
                   json_payload) 
      SELECT @EventID                          AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date,              
			 SFCode							   AS SFCode,
			 (select 
				 getutcdate() as Date1
				,@EventID as [Event]
				,SFCode  as PropertyID
				,CAST(0 as BIT) as IsForce
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   SF_data 
		
		
declare @message varchar(200) = 'Finished Inserting Records into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO



-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_SFStatus_OfferRejected.sql

GO


/******************************************************************************************* 
Pre Acquisition Property Marked Exclude or no longer Exists - Field Services Event Fire
Description:	Checks Hub.dbo.Property for Excluded properties and current projects where the property has been removed.
*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_SFStatus_OfferRejected
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'PFS_SFStatus_OfferRejected'
exec Hub.dbo.PFS_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking Pre Acq Status for Rejected\Exclude or Property no longer exists'

Declare @EventId int = 206;
Declare @SourceId int = 1;

with  SF_data as (
	select
	 top 3 
		re.SFCode 
	from 
	 Hub.dbo.view_PFS_EventStatus re
	 LEFT JOIN hub.dbo.property p  on re.SFCode = p.SFCode
	where 
		re.ProjectType = 'Reno' AND re.SFCode is not null
		AND (sPreAcqStatus = 'Exclude' or P.SFCode is null )  --Excluded or doesn't exist
		AND isnull(re.Created, 0) = 1 --But project status is created
)
   INSERT INTO hub.dbo.PFS_eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   SFCode, 
                   json_payload) 
      SELECT @EventID                          AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date,              
			 SFCode							   AS SFCode,
			 (select 
				 getutcdate() as Date1
				,@EventID as [Event]
				,SFCode  as PropertyID
				,CAST(0 as BIT) as IsForce
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   SF_data 
		
		
declare @message varchar(200) = 'Finished Inserting Records into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO



-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_UnitStatus_CorpRenewal.sql

GO


/******************************************************************************************* 
Resident Notice to Move Out - Field Services Event Fire
Description:	This procedure will write a record to the Hub.dbo.PFS_eventlog table when a property's
				unit status changes to the designated status.
*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_UnitStatus_CorpRenewal
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'PFS_UnitStatus_CorpRenewal'
exec Hub.dbo.PFS_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking Unit Status for Corporate Renewal Complete'


Declare @CorpRenewEventId int = 3;
Declare @CreateEventId int = 1;
Declare @SourceId int = 1;

with prev_corp_renew_event as 
(
	SELECT 
		voyager_property_hmy 
		,max(load_date) as LastEventDate
    FROM   hub.dbo.PFS_eventlog 
	WHERE Event_ID = @CorpRenewEventId
	GROUP BY Voyager_Property_HMY
	HAVING max(load_date) >= convert(date, dateadd(day, -120, getdate()))
)
, yardi_data as (
select 
top 3	
	yd.YardiId
	,yd.YardiScode
	,yd.UnitStatus	
	,ce.LastCreateDate
from 
Homes.dbo.vYardi_PropertyLevel_YD yd
LEFT JOIN Hub.dbo.view_PFS_EventStatus ce on ce.Voyager_Property_HMY = yd.YardiId
LEFT JOIN prev_corp_renew_event ren on ren.Voyager_Property_HMY = yd.YardiId
where 
yd.UnitStatus in ('Vacant Rented Ready','Vacant Not Rented Ready','Occupied No Notice','Down')
and ce.Created = 1	--Make sure the create event has been sent previously.
and (ren.LastEventDate is null OR ren.LastEventDate < ce.LastCreateDate)  --Corp renewal never sent or it was sent before last time project was created.
)
   INSERT INTO hub.dbo.PFS_eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   voyager_property_hmy, 
				   voyager_property_scode,
                   json_payload) 
      SELECT @CorpRenewEventId				   AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date, 
             YardiId, 
			 YardiScode,
			 (select 
				 getutcdate() as Date1
				,@CorpRenewEventId as [Event]
				,YardiScode  as PropertyID
				,CAST(0 as BIT) as IsForce
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   yardi_data 
		
declare @message varchar(200) = 'Finished Inserting Records for Corp Renewal Complete into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO



-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_UnitStatus_NoticeSent.sql

GO


/******************************************************************************************* 
Resident Notice to Move Out - Field Services Event Fire
Description:	This procedure will write a record to the Hub.dbo.PFS_eventlog table when a property's
				unit status changes to the designated status.
*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_UnitStatus_NoticeSent
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'PFS_UnitStatus_NoticeSent'
exec Hub.dbo.PFS_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking Unit Status for Notice Sent'

Declare @EventId int = 1;
Declare @SourceId int = 1;

with  yardi_data as (
select 
top 3
	datediff(day, yd.CurrentUnitStatusBegin, getdate()) as DaysSinceStatusChange 
	,yd.YardiId
	,yd.YardiScode
	,yd.UnitStatus
	,yd.CurrentUnitStatusBegin	
from 
Homes.dbo.vYardi_PropertyLevel_YD yd
LEFT JOIN Hub.dbo.view_PFS_EventStatus re on re.Voyager_Property_HMY = yd.YardiId
where 
yd.UnitStatus in ('Notice Unrented','Notice Rented','Vacant Unrented Not Ready','Vacant Rented Not Ready')
--Either the unit status begin date is null or the event happened recently
and (CurrentUnitStatusBegin  is null OR (CurrentUnitStatusBegin <= convert(date, getdate()) and datediff(day, CurrentUnitStatusBegin, getdate()) < 15))
and isnull(re.Created, 0) = 0
)
   INSERT INTO hub.dbo.PFS_eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   voyager_property_hmy, 
				   voyager_property_scode,
                   json_payload) 
      SELECT @EventID                          AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date, 
             YardiId, 
			 YardiScode,
			 (select 
				isnull(Homes.dbo.fncs_ConvertESTtoUTC(CurrentUnitStatusBegin), getutcdate()) as Date1
				,@EventID as [Event]
				,YardiScode  as PropertyID
				,CAST(0 as BIT) as IsForce
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   yardi_data 
		
declare @message varchar(200) = 'Finished Inserting Records into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO



-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Stored Procedures\PFS_YardiContract_Submit.sql

GO


--/******************************************************************************************* 
--Description:	This procedure will write a record to the Hub.dbo.PFS_eventlog table when a property's
--				has a new yardi contract\job and has been sent to Dynamics.
--*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_YardiContract_Submit
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'PFS_YardiContract_Submit'
exec Hub.dbo.PFS_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking for new yardi jobs.'

Declare @CreateEventId int = 1;
Declare @EventId int = 10;
Declare @SourceId int = 1;

with recent_event_job as (
	SELECT 
		voyager_property_hmy 
		,max(load_date) as LastEventDate
    FROM   hub.dbo.PFS_eventlog 
	WHERE Event_ID = @EventId
	GROUP BY Voyager_Property_HMY
	HAVING max(load_date) >= convert(date, dateadd(day, -120, getdate()))
)
,job_data as (
	select 		
		j.HPROPERTY
		,j.SSCHEDDATE
		,j.DTCREATED
		,j.dtStartActual
		,j.dtStartRevised	
		,j.SCODE as JobScode
	from 
	 BLTYardi.dbo.job j
	where  isnull(j.dtStartActual,dtStartRevised) >= dateadd(day,-7,getdate())	
)
, yardi_data as (
select 
top 3	--For Testing
	yd.YardiId
	,yd.YardiScode
	,yd.UnitStatus		
	,jd.*
from 
	Homes.dbo.vYardi_PropertyLevel_YD yd
	inner join job_data jd on jd.HPROPERTY = yd.YardiId
	LEFT JOIN Hub.dbo.view_PFS_EventStatus re on re.Voyager_Property_HMY = yd.YardiId
	left join recent_event_job rej on rej.Voyager_Property_HMY = yd.YardiId
where 
	re.Created = 1 --make sure create project created
	and rej.LastEventDate is null --but action job event (10) not recently sent
)

   INSERT INTO hub.dbo.PFS_eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   voyager_property_hmy, 
				   voyager_property_scode,
                   json_payload) 
      SELECT @EventID                          AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date, 
             YardiId, 
			 YardiScode,
			  (select 
				Homes.dbo.fncs_ConvertESTtoUTC(DTCREATED) as Date1
				,@EventID as [Event]
				,YardiScode  as PropertyID
				,CAST(0 as BIT) as IsForce
				,rtrim(JobScode) as JobID
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   yardi_data 
		
declare @message varchar(200) = 'Finished Inserting Job Created into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO


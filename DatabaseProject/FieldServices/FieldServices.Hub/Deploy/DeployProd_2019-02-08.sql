USE [Hub];
GO
CREATE OR ALTER  VIEW  [dbo].view_PFS_EventStatus AS

with last_event_create as (
	SELECT 
		isnull(EL.voyager_property_hmy ,p.YardihMy) as voyager_property_hmy
		,EL.SFCode
		,max(EL.load_date) as LastEventDate
		,EL.Event_ID		
    FROM   hub.dbo.PFS_eventlog EL 
	LEFT JOIN hub.dbo.property p 
		on (el.SFCode = p.SFCode or el.SFCode is null) AND (el.Voyager_Property_HMY is null or el.Voyager_Property_HMY = p.YardihMy)
		AND (el.SFCode is not null OR el.Voyager_Property_HMY is not null)
	WHERE EL.Event_ID = 1  --Notice Sent Event
		  OR EL.Event_ID = 201 --Offer Accepted
	GROUP BY EL.Voyager_Property_HMY, p.YardihMy, EL.SFCode, EL.Event_ID
)
,last_event_renew as (
	SELECT 
		EL.voyager_property_hmy as voyager_property_hmy
		,EL.SFCode
		,max(EL.load_date) as LastEventDate
    FROM   hub.dbo.PFS_eventlog EL		
	WHERE EL.Event_ID = 3  --Corp Renew Event
	OR EL.Event_ID = 206  --Offer rejected
	GROUP BY EL.Voyager_Property_HMY, EL.SFCode
)
select 
	ec.Voyager_Property_HMY
	,ec.SFCode
	,CASE WHEN er.LastEventDate is null THEN 1
		WHEN er.LastEventDate < ec.LastEventDate THEN 1
		WHEN er.LastEventDate >= ec.LastEventDate THEN 0
	ELSE 0 END as Created
	,CASE WHEN er.LastEventDate is null THEN 'Created'
		WHEN er.LastEventDate < ec.LastEventDate THEN 'Cancelled Prev to Create'
		WHEN er.LastEventDate >= ec.LastEventDate THEN 'Cancelled'
	ELSE 'Not Created' END as [Status]
	,CASE WHEN ec.Event_ID = 1 THEN 'Turn'
		WHEN ec.Event_ID = 201 THEN 'Reno'		
	ELSE NULL END as [ProjectType]
	,ec.LastEventDate as LastCreateDate
	,er.LastEventDate as LastCancelDate	
from 
last_event_create ec
left join
last_event_renew er		--Either match on HMY or HMY is null so match on SFCode
	on (ec.Voyager_Property_HMY = er.Voyager_Property_HMY AND ec.voyager_property_hmy is not null)
		OR (ec.SFCode = er.SFCode AND ec.SFCode is not null)
where
ec.LastEventDate is not null

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
				dateadd(MINUTE,datepart(TZOFFSET, MoveOutDate AT TIME ZONE 'Eastern Standard Time') *-1,  MoveOutDate) as Date1
				,@EventID as [Event]
				,YardiScode  as PropertyID
				,CAST(0 as BIT) as IsForce
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   yardi_data 
		
declare @message varchar(200) = 'Finished Inserting Move Out Changed into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
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
				iif(CurrentUnitStatusBegin is null, 
					getutcdate(),
					dateadd(MINUTE,datepart(TZOFFSET, CurrentUnitStatusBegin AT TIME ZONE 'Eastern Standard Time') *-1,  CurrentUnitStatusBegin)) as Date1 --Convert to UTC
				,@EventID as [Event]
				,YardiScode  as PropertyID
				,CAST(0 as BIT) as IsForce
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   yardi_data 
		
declare @message varchar(200) = 'Finished Inserting Records into  hub.dbo.PFS_eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.PFS_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
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
				dateadd(MINUTE,datepart(TZOFFSET, DTCREATED AT TIME ZONE 'Eastern Standard Time') *-1,  DTCREATED) as Date1 --Convert to UTC
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






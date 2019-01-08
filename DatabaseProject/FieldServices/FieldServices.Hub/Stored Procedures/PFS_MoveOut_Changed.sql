USE [Hub]
GO

--/******************************************************************************************* 
--Resident Notice to Move Out - Field Services Event Fire
--Description:	This procedure will write a record to the Hub.dbo.EventLog table when a property's
--				unit status changes to the designated status.
--*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_MoveOut_Changed
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'PFS_MoveOut_Changed'
exec Hub.dbo.stp_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking for move out date changed.'

Declare @CreateEventId int = 1;
Declare @EventId int = 1001;
Declare @SourceId int = 1;

;with movedate_events as
(
	SELECT 		
		*		
		,ROW_NUMBER() OVER (PARTITION BY el.Voyager_Property_HMY  ORDER BY el.load_date desc) AS RID
    FROM   hub.dbo.eventlog el
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
   INSERT INTO hub.dbo.eventlog 
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
		
declare @message varchar(200) = 'Finished Inserting Move Out Changed into  hub.dbo.eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.stp_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO

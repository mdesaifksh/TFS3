USE [Hub]
GO


/******************************************************************************************* 
Resident Notice to Move Out - Field Services Event Fire
Description:	This procedure will write a record to the Hub.dbo.EventLog table when a property's
				unit status changes to the designated status.
*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].FS_UnitStatus_NoticeSent
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'FS_UnitStatus_NoticeSent'
exec Hub.dbo.stp_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking Unit Status for Notice Sent'

Declare @EventId int = 1;
Declare @SourceId int = 1;

with recent_event as (
	SELECT 
		voyager_property_hmy 
		,max(load_date) as LastEventDate
    FROM   hub.dbo.eventlog 
	WHERE Event_ID = @EventId
	GROUP BY Voyager_Property_HMY
)
, yardi_data as (
select 
top 3
	datediff(day, yd.UnitStatusBegin, getdate()) as DaysSinceStatusChange 
	,yd.YardiId
	,yd.YardiScode
	,yd.UnitStatus
	,yd.UnitStatusBegin
	,re.LastEventDate
from 
Homes.dbo.vYardi_PropertyLevel_YD yd
LEFT JOIN recent_event re on re.Voyager_Property_HMY = yd.YardiId
where 
yd.UnitStatus in ('Notice Unrented','Notice Rented','Vacant Unrented Not Ready','Vacant Rented Not Ready')
and UnitStatusBegin <= convert(date, getdate()) and datediff(day, UnitStatusBegin, getdate()) < 15
and LastEventDate is null
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
				UnitStatusBegin as Date1
				,@EventID as [Event]
				,YardiScode  as PropertyID
				,CAST(0 as BIT) as IsForce
				FOR JSON PATH, WITHOUT_ARRAY_WRAPPER) as Json_Payload
        FROM   yardi_data 
		
declare @message varchar(200) = 'Finished Inserting Records into  hub.dbo.eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.stp_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO



USE [Hub]
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



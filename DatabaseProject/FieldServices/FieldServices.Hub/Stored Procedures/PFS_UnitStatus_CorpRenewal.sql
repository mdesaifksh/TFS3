USE [Hub]
GO


/******************************************************************************************* 
Resident Notice to Move Out - Field Services Event Fire
Description:	This procedure will write a record to the Hub.dbo.EventLog table when a property's
				unit status changes to the designated status.
*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_UnitStatus_CorpRenewal
AS
BEGIN

SET NOCOUNT ON;

	
declare @SprocName varchar(100) = 'PFS_UnitStatus_CorpRenewal'
exec Hub.dbo.stp_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking Unit Status for Corporate Renewal Complete'


Declare @CorpRenewEventId int = 3;
Declare @CreateEventId int = 1;
Declare @SourceId int = 1;

with prev_corp_renew_event as 
(
	SELECT 
		voyager_property_hmy 
		,max(load_date) as LastEventDate
    FROM   hub.dbo.eventlog 
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
   INSERT INTO hub.dbo.eventlog 
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
		
declare @message varchar(200) = 'Finished Inserting Records for Corp Renewal Complete into  hub.dbo.eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.stp_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO



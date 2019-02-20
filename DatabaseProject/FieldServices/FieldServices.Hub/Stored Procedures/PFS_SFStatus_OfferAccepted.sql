USE [Hub]
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



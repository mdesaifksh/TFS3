USE [Hub]
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
exec Hub.dbo.stp_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking Pre Acq Status for Acquired'

Declare @EventId int = 206;
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
   INSERT INTO hub.dbo.eventlog 
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
		
		
declare @message varchar(200) = 'Finished Inserting Records into  hub.dbo.eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.stp_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;

END
GO



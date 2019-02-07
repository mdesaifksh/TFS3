USE [Hub]
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


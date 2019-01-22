USE [Hub]
GO


--/******************************************************************************************* 
--Description:	This procedure will write a record to the Hub.dbo.EventLog table when a property's
--				has a new yardi contract\job and has been sent to Dynamics.
--*******************************************************************************************/
CREATE OR ALTER PROCEDURE [dbo].PFS_Renowalk_Status
AS
BEGIN

SET NOCOUNT ON;

declare @SprocName varchar(100) = 'PFS_Renowalk_Status'
exec Hub.dbo.stp_LogMessage @LogLevel=4, @SprocName = @SprocName, @Message ='Checking for approved budgets.'

Declare @BudgetApprovedEventIdTurn int = 8;
Declare @BudgetApprovedEventIdReno int = 205;
Declare @SourceId int = 1;

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
    FROM   hub.dbo.eventlog 
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
   INSERT INTO hub.dbo.eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   voyager_property_hmy, 
				   voyager_property_scode,
                   json_payload) 
      SELECT @BudgetApprovedEventIdTurn                          AS Event_ID, 
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
		
declare @message varchar(200) = 'Finished Inserting Approved budget events into  hub.dbo.eventlog : Count:' + format(@@ROWCOUNT, 'N0');
exec Hub.dbo.stp_LogMessage @LogLevel=3, @SprocName = @SprocName, @Message = @message;


END
GO


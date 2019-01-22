
USE [Hub];
GO
CREATE OR ALTER  VIEW  [dbo].view_PFS_EventStatus AS

with last_event_create as (
	SELECT 
		voyager_property_hmy 
		,max(load_date) as LastEventDate
		,Event_ID
    FROM   hub.dbo.eventlog 
	WHERE Event_ID = 1  --Notice Sent Event
		  OR Event_ID = 201 --Offer Accepted
	GROUP BY Voyager_Property_HMY, Event_ID
)
,last_event_renew as (
	SELECT 
		voyager_property_hmy 
		,max(load_date) as LastEventDate
    FROM   hub.dbo.eventlog 
	WHERE Event_ID = 2  --Corp Renew Event
	GROUP BY Voyager_Property_HMY
)
select 
	ec.Voyager_Property_HMY
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
last_event_renew er on ec.Voyager_Property_HMY = er.Voyager_Property_HMY
where
ec.LastEventDate is not null

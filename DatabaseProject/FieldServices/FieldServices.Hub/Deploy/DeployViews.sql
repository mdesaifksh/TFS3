
-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Views\dbo.view_PFS_EventIncomingLog.sql

USE [Hub];
GO
CREATE OR ALTER  VIEW  [dbo].view_PFS_EventIncomingLog AS

with trimmed as (
	select 
		ID
		,Topic
		,Message_Subject
		,Message_Sent
		,SUBSTRING(trim(Json_Payload), 2, LEN(trim(Json_Payload))-2) as Json_Payload_Trimmed
		,CreatedAt		
		,ISJSON(SUBSTRING(trim(Json_Payload), 2, LEN(trim(Json_Payload))-2)) as Json_Payload_Trim_IsJson		
	from Hub.dbo.PFS_EventIncomingLog 
	where Json_Payload is not null
)
select 
	ID
	,Topic
	,Message_Subject
	,Message_Sent
	,Json_Payload_Trimmed
	,CreatedAt
	,Json_Payload_Trim_IsJson
	,iif(Json_Payload_Trim_IsJson = 1, Convert(datetime, JSON_VALUE(Json_Payload_Trimmed, '$.Date1')), null) as Payload_Date1
	,iif(Json_Payload_Trim_IsJson = 1, Convert(int, JSON_VALUE(Json_Payload_Trimmed, '$.Event')), null) as Payload_Event
	,iif(Json_Payload_Trim_IsJson = 1, JSON_VALUE(Json_Payload_Trimmed, '$.PropertyID'), null) as Payload_PropertyID
from trimmed

-- Appending File: C:\Projects\git\ProjectAndFieldServices\DatabaseProject\FieldServices\FieldServices.Hub\Views\dbo.view_PFS_EventStatus.sql

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
	WHERE EL.Event_ID = 2  --Corp Renew Event
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
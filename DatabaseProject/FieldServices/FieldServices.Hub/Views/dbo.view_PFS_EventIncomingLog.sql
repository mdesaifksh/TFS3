
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

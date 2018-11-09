USE BLTYardi
GO

/******************************************************************************************* 
60 Day Notice Event Log entry
Created Date:	11/09/2018
Created By:		Mike Ameye
Description:	Procedure to pull all tenant lease end dates that are within 60 days of ending.

Change Log:
1.	11/09/2018	MA	Initial Creation


*******************************************************************************************/

DECLARE @JSonText VARCHAR(1000), 
		@hMy NUMERIC(18,0),
		@DTLeaseTo VARCHAR(10),
		@EventID INT,
		@SourceID INT,
		@LagDays INT  

SET @EventID = 1
SET @SourceID = 1
SET @LagDays = 60


DECLARE p CURSOR FOR 
	SELECT	bt.HPROPERTY
	       , CONVERT(VARCHAR(10), bt.DTLEASETO, 101)
	from BLTYardi.dbo.PROPERTY yp
		JOIN BLTYardi.dbo.TENANT as bt with (nolock) ON bt.HPROPERTY = yp.HMY
		JOIN BLTYardi.dbo.tenstatus	as bts with (nolock) on bts.istatus = bt.ISTATUS
	WHERE bt.DTLEASETO = CONVERT(VARCHAR(10), GETDATE() + @LagDays, 101)
		AND bts.status = 'Current'
		AND NOT EXISTS (SELECT Voyager_Property_HMY FROM Hub.dbo.EventLog WHERE Load_Date >= GETDATE() - 60 AND Event_ID = @EventID AND Voyager_Property_HMY = bt.HPROPERTY)

OPEN p  
FETCH NEXT FROM p INTO @hMy, @DTLeaseTo  
WHILE @@FETCH_STATUS=0  
BEGIN  
	-- Generate JSON code
	SET @JSonText = '{"Date1":"'+@dtLeaseTo+'","Event":"'+CONVERT(VARCHAR(1),@EventID)+'","IsForce":false,"PropertyID":"'+CONVERT(VARCHAR(6),@hMy)+'"}'

	--Now insert record into Event table
	--SELECT @JSonText
	INSERT INTO Hub.dbo.EventLog ( Event_ID, Source_ID, Load_Date, Voyager_Property_HMY, Json_Payload)
	VALUES ( @EventID, @SourceID, GETDATE(), @hMy, @JSonText )

	-- Get next value
	FETCH NEXT FROM p INTO @hMy, @DTLeaseTo   

END  
CLOSE p  
DEALLOCATE p  

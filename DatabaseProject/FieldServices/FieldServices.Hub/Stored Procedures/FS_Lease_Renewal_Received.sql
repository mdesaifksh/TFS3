USE [Hub]
GO

/****** Object:  StoredProcedure [dbo].[FS_Lease_Renewal_Received]    Script Date: 11/14/2018 1:14:30 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



/******************************************************************************************* 
Lease Renewal Received Log entry
Created Date:	11/14/2018
Created By:		Vinod Eppa
Description:	Procedure to pull all tenants who renewed their lease in last 60 days
Change Log:
1.	11/14/2018	VE	Initial Creation

********************************************************************************************
Exec FS_Lease_Renewal_Received
*******************************************************************************************/
CREATE PROCEDURE [dbo].[FS_Lease_Renewal_Received] 
AS 
  BEGIN 
      -- SET NOCOUNT ON added to prevent extra result sets from  
      -- interfering with SELECT statements.  
      SET nocount ON; 

      INSERT INTO hub.dbo.eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   voyager_property_hmy, 
                   json_payload) 
      SELECT 2                                          AS Event_ID, 
             1                                          AS Source_ID, 
             Getdate()                                  AS Load_Date, 
             bt.hproperty                               AS HMY, 
             '{"Date1":"' 
             + CONVERT(VARCHAR(10), bt.dtrenewdate, 101) 
             + '","Event":"' + CONVERT(VARCHAR(1), 2) 
             + '","IsForce":false,"PropertyID":"' 
             + CONVERT(VARCHAR(6), bt.hproperty) + '"}' AS Json_Payload 
      FROM   bltyardi.dbo.property yp 
             JOIN bltyardi.dbo.tenant AS bt WITH (nolock) 
               ON bt.hproperty = yp.hmy 
             JOIN bltyardi.dbo.tenstatus AS bts WITH (nolock) 
               ON bts.istatus = bt.istatus 
      WHERE  1 = 1 
             AND Isnull(bt.dtrenewdate, '') >= bt.dtleaseto 
             AND bt.dtleaseto >= CONVERT(VARCHAR(10), Getdate(), 101) 
             AND bts.status = 'Current' 
             AND NOT EXISTS (SELECT voyager_property_hmy 
                             FROM   hub.dbo.eventlog 
                             WHERE  load_date <= Getdate() - 60 
                                    AND event_id = 2 
                                    AND voyager_property_hmy = bt.hproperty) 
  END 
	
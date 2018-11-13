USE [Hub]
GO

/****** Object:  StoredProcedure [dbo].[Fs_xday_notice]    Script Date: 11/13/2018 4:58:40 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO



/******************************************************************************************* 
30 Day Notice Event Log entry
Created Date:	11/13/2018
Created By:		Vinod Eppa
Description:	Procedure to pull all tenant lease end dates that are within 30 or 60 days of ending.
Change Log:
1.	11/13/2018	VE	Initial Creation

********************************************************************************************
Exec [dbo].[FS_XDay_Notice] 1,60

Exec [dbo].[FS_XDay_Notice] 3,30

*******************************************************************************************/
CREATE PROCEDURE [dbo].[Fs_xday_notice] @EventID INT, 
                                        @LagDays INT 
AS 
  BEGIN 
      -- SET NOCOUNT ON added to prevent extra result sets from 
      -- interfering with SELECT statements. 
      SET nocount ON; 

      DECLARE @SourceID INT 

      SET @SourceID = 1;

      WITH CTE_Prop (hmy, dtleaseto) 
           AS (SELECT bt.hproperty, 
                      CONVERT(VARCHAR(10), bt.dtleaseto, 101) 
               FROM   bltyardi.dbo.property yp 
                      JOIN bltyardi.dbo.tenant AS bt WITH (nolock) 
                        ON bt.hproperty = yp.hmy 
                      JOIN bltyardi.dbo.tenstatus AS bts WITH (nolock) 
                        ON bts.istatus = bt.istatus 
               WHERE  bt.dtleaseto = CONVERT(VARCHAR(10), Getdate() + @LagDays, 
                                     101) 
                      AND bts.status = 'Current' 
                      AND NOT EXISTS (SELECT voyager_property_hmy 
                                      FROM   hub.dbo.eventlog 
                                      WHERE  load_date >= Getdate() - @LagDays 
                                             AND event_id = @EventID 
                                             AND voyager_property_hmy = 
                                                 bt.hproperty)) 
      INSERT INTO hub.dbo.eventlog 
                  (event_id, 
                   source_id, 
                   load_date, 
                   voyager_property_hmy, 
                   json_payload) 
      SELECT @EventID                          AS Event_ID, 
             @SourceID                         AS Source_ID, 
             Getdate()                         AS Load_Date, 
             hmy, 
             '{"Date1":"'+dtLeaseTo+'","Event":"'+CONVERT(VARCHAR(1),@EventID)+'","IsForce":false,"PropertyID":"'+CONVERT(VARCHAR(6),hMy)+'"}' AS Json_Payload
      FROM   CTE_Prop 
  END 
GO



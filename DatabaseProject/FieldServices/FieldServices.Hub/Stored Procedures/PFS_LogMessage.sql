USE HUB
GO

Create or Alter PROCEDURE dbo.stp_LogMessage
	 @LogLevel AS Int
	,@SprocName as VARCHAR(200)
	,@Message AS VARCHAR(Max)
	,@AppName as VARCHAR(200) = 'Hub'
	,@Exception AS VARCHAR(Max) = null
	,@DetailsJson AS VARCHAR(MAX) = null
	,@User AS VARCHAR(100) = null
	,@MachineName AS VARCHAR(100) = null
	,@LogDate As Datetime = null
AS

BEGIN 

SET @LogDate	= COALESCE(@LogDate,GETDATE())
SET @User		= COALESCE(@User,SUSER_NAME())
SET @MachineName = COALESCE(@MachineName, @@SERVERNAME)

--Make sure Log Level passed in exists in LoggingLevels, otherwise default it to 4 (Info)
select @LogLevel = ll.Id
from hub.dbo.LoggingLevels ll
where ll.Id = @LogLevel

set @LogLevel = COALESCE(@LogLevel, 4)

INSERT INTO [dbo].[Logging]
           ([LogDate]
           ,[AppName]
           ,[SprocName]
           ,[User]
           ,[Level]
           ,[MachineName]
           ,[Message]
           ,[Exception]
           ,[DetailsJson])
     VALUES
           (@LogDate
           ,@AppName
           ,@SprocName
           ,@User
           ,@LogLevel
           ,@MachineName
           ,@Message
           ,@Exception
           ,@DetailsJson)

END
GO


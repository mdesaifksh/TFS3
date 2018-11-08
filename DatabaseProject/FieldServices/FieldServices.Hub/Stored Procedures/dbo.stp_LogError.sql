USE HUB
GO

Create or Alter PROCEDURE dbo.stp_LogError
	 @LogLevel AS Int
	,@SprocName as VARCHAR(200)
	,@Message AS VARCHAR(Max)
	,@AppName as VARCHAR(200) = 'Hub'	
	,@DetailsJson AS VARCHAR(MAX) = null
	,@User AS VARCHAR(100) = null
	,@MachineName AS VARCHAR(100) = null
	,@LogDate As Datetime = null
AS

BEGIN 
 
DECLARE @ErrorNumber INT = ERROR_NUMBER();
DECLARE @ErrorLine INT = ERROR_LINE();
DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
DECLARE @ErrorState INT = ERROR_STATE();

Declare @Exception Varchar(5000)
Set @Exception = 
	'Number:' + Convert(varchar(200), @ErrorNumber) 
	+ '; Line:' + Convert(varchar(200), @ErrorLine)
	+ '; Severity:' + Convert(varchar(200), @ErrorSeverity)
	+ '; State:' + Convert(varchar(200), @ErrorState)
	+ '; Message:' + @ErrorMessage;

exec stp_LogMessage @LogLevel, @SprocName, @Message, @AppName, @Exception, @DetailsJson, @User, @MachineName, @LogDate

END
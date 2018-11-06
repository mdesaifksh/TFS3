USE HUB
GO

Create or Alter PROCEDURE Hub.dbo.stp_LogMessage
	 @LogLevel AS Int
	,@Message AS VARCHAR(Max)

AS
BEGIN
select * from [FileIndex] FI
where
	FI.BasePath like '%' + @BasePath 
	AND FI.RelativeDirectory = @Directory
END
GO


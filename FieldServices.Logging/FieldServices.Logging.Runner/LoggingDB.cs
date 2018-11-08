using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using NLog;

namespace FieldServices.Logging.Runner
{
    public static class LoggingDB
    {
        private readonly static Logger _logger = LogManager.GetCurrentClassLogger();
        public static List<LogMessage> GetLogMessagesSinceLastUpdate(DateTime date)
        {
            var connectionString = System.Configuration.ConfigurationManager.AppSettings["SqlConn"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentNullException("Connection String");
            }

            string sql = @"select [LogDate]
      ,[AppName]
      ,[SprocName]
      ,[User]
      , ll.Name[Level]
      ,[MachineName]
      ,[Message]
      ,[Exception]
      ,[DetailsJson]
        from
    Hub.dbo.Logging l (nolock)
inner join Hub.dbo.LoggingLevels ll on l.Level = ll.id   where LogDate > @lastDate";

            using (var connection = new SqlConnection(connectionString))
            {
                var logMessages = connection.Query<LogMessage>(sql, commandType: CommandType.Text, param: new { lastDate = ConvertTimezoneToEST(date)}).ToList();
                FixTimeZone(logMessages);
                return logMessages;
            }

        }

        /// <summary>
        /// Dates are stored in DB as EST, these are not being handled correctly on servers with UTC as local time zone.
        /// So take offset between
        /// </summary>
        /// <param name="messages"></param>
        public static void FixTimeZone(List<LogMessage> messages)
        {
            var localOffset = DateTimeOffset.Now.Offset;
            var estOffset = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time").BaseUtcOffset;
            var offsetDif = localOffset - estOffset;
           // _logger.Info($"FixTimeZone: OffsetDif: {offsetDif}");
            foreach (var item in messages)
            {
                item.LogDate = item.LogDate + offsetDif;
            }
        }

        /// <summary>
        /// Convert date back to est.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private static DateTime ConvertTimezoneToEST(DateTime date)
        {
            var localOffset = DateTimeOffset.Now.Offset;
            var estOffset = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time").BaseUtcOffset;
            var offsetDif = localOffset - estOffset;
           // _logger.Info($"ConvertTimezoneToEST: offsetDif: {offsetDif}");
            date = date - offsetDif;
            return date;
        }
    }
}

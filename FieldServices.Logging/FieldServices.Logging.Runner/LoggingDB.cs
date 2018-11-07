using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace FieldServices.Logging.Runner
{
    public static class LoggingDB
    {
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
                var logMessages = connection.Query<LogMessage>(sql, commandType: CommandType.Text, param: new { lastDate = date }).ToList();
                return logMessages;
            }
        }
    }
}

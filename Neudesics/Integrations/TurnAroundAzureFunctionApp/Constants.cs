using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TurnAroundAzureFunctionApp
{
    public class Constants
    {
        public static class AzureIntegrationCalls
        {
            public const string LogicalName = "fkh_azureintegrationcall";
            public const string PrimaryKey = "fkh_azureintegrationcallid";

            public const string EventName = "fkh_name";
            public const string Direction = "fkh_direction";
            public const string EventData = "fkh_eventdata";
            /// <summary>
            /// To Be Processed - 1
            /// Completed - Successfully - 963850000
            /// Completed - Errors - 963850001
            /// Completed - Failed - 963850002
            /// </summary>
            public const string StatusCode = "statuscode";
            public const string ErrorDetails = "fkh_errordetails";
        }
    }
}

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

        }
    }
}

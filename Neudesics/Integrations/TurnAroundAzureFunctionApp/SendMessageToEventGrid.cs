using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Dynamic;
using System;
using System.IO;
using System.Text;

namespace TurnAroundAzureFunctionApp
{
    public static class SendMessageToEventGrid
    {
        [FunctionName("SendMessageToEventGrid")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            var content = req.Content;

            string jsonContent = await content.ReadAsStringAsync();
            //log.Info($"Received D365 Event with payload: {jsonContent}");

            Microsoft.Xrm.Sdk.RemoteExecutionContext remoteExecutionContext = DeserializeJsonString<Microsoft.Xrm.Sdk.RemoteExecutionContext>(jsonContent);


            //read Plugin Message Name
            string messageName = remoteExecutionContext.MessageName;
            log.Info($"Message Name : {messageName}");
            //read execution depth of plugin
            Int32 depth = remoteExecutionContext.Depth;
            log.Info($"Depth : {depth}");
            //read BusinessUnitId
            Guid businessUnitid = remoteExecutionContext.BusinessUnitId;
            log.Info($"Business Unit ID  : {businessUnitid.ToString()}");
            //read Target Entity 
            Microsoft.Xrm.Sdk.Entity targetEntity = (Microsoft.Xrm.Sdk.Entity)remoteExecutionContext.InputParameters["Target"];
            log.Info($"Target Entity Logical Name   : {targetEntity.LogicalName} - ID : {targetEntity.Id.ToString()}");

            //read attribute from Target Entity
            string dataPayload = targetEntity.GetAttributeValue<string>("fkh_eventdata");
            log.Info($"Data PayLoad : {dataPayload}");


            return jsonContent == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, "Hello " + jsonContent);
        }

        /// <summary>
        /// Function to deserialize JSON string using DataContractJsonSerializer
        /// </summary>
        /// <typeparam name="RemoteContextType">RemoteContextType Generic Type</typeparam>
        /// <param name="jsonString">string jsonString</param>
        /// <returns>Generic RemoteContextType object</returns>
        public static RemoteContextType DeserializeJsonString<RemoteContextType>(string jsonString)
        {
            //create an instance of generic type object
            RemoteContextType obj = Activator.CreateInstance<RemoteContextType>();
            MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(jsonString));
            System.Runtime.Serialization.Json.DataContractJsonSerializer serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(obj.GetType());
            obj = (RemoteContextType)serializer.ReadObject(ms);
            ms.Close();
            return obj;
        }
    }
}

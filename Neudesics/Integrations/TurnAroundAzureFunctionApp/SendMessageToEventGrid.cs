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
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Crm.Sdk.Messages;

namespace TurnAroundAzureFunctionApp
{
    public static class SendMessageToEventGrid
    {
        //const string TOPIC_ENDPOINT = "https://pfs-d365tosqlhubeventgridtopic.eastus-1.eventgrid.azure.net/api/events";
        //const string SAS_KEY = "uCQi10m3cngHbqJ7zBm6m3aBWz5DVQqgC+fjGRuoPWE=";


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

            if (!string.IsNullOrEmpty(dataPayload))
            {
                log.Info($"Sending Event to EventGrid Topic...");
                callTopicAsync(log, dataPayload, targetEntity.ToEntityReference()).GetAwaiter().GetResult();
                log.Info($"Event successfully sent to EventGrid Topic...");
            }

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

        static async Task callTopicAsync(TraceWriter log, string jsonContent, EntityReference azrIntegrationCallEntityReference)
        {
            // string topicEndpoint = "https://turnaroundeventgridtopic.eastus-1.eventgrid.azure.net/api/events";
            //string sasKey = "v6xbKQuwLMjh7fus4KJ+4plVXDEQJlkuFswZAlzEdjM=";

            Console.WriteLine("================================================");
            Console.WriteLine("Press any key to exit after sending the message.");
            Console.WriteLine("================================================");

            string sas_key = Environment.GetEnvironmentVariable("D365ToSQL_SAS_Key");
            log.Info($"SAS KEY : {sas_key}");
            string topic_EndPoint = Environment.GetEnvironmentVariable("D365ToSQL_Topic_EndPoint");
            log.Info($"TOPIC ENDPOINT : {topic_EndPoint}");
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("aeg-sas-key", sas_key);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("democlient");


            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, topic_EndPoint)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };
            

            HttpResponseMessage response = await client.SendAsync(request);
            log.Info($"Response StatusCode : {response.StatusCode.ToString()}");
            log.Info($"Response IsSuccessStatusCode : {response.IsSuccessStatusCode.ToString()}");
            log.Info($"Response ReasonPhrase : {response.ReasonPhrase}");

            //OrganizationWebProxyClient _service = CRMCall(log);
            //if (_service is OrganizationWebProxyClient)
            //{
            //    if (response.IsSuccessStatusCode)
            //    {
            //        //Update Azure Integration Record with Success...
            //        Entity azrInt = new Entity(azrIntegrationCallEntityReference.LogicalName);
            //        azrInt.Id = azrIntegrationCallEntityReference.Id;
            //        azrInt[Constants.AzureIntegrationCalls.StatusCode] = new OptionSetValue(963850000);
            //        azrInt[Constants.AzureIntegrationCalls.ErrorDetails] = null;

            //        _service.Update(azrInt);
            //    }
            //    else
            //    {
            //        //Update Azure Integration Record with Failure and add Error Message.
            //        Entity azrInt = new Entity(azrIntegrationCallEntityReference.LogicalName);
            //        azrInt.Id = azrIntegrationCallEntityReference.Id;
            //        azrInt[Constants.AzureIntegrationCalls.StatusCode] = new OptionSetValue(963850001);
            //        azrInt[Constants.AzureIntegrationCalls.ErrorDetails] = $"Status Code : {response.StatusCode.ToString()} {Environment.NewLine} Error Reason : {response.ReasonPhrase}";

            //        _service.Update(azrInt);
            //    }
            //}
            Console.WriteLine("================================================");
            Console.WriteLine("Message successfully sent.");
            Console.WriteLine("================================================");
        }

        private static OrganizationWebProxyClient CRMCall(TraceWriter log)
        {
            var aadInstance = "https://login.microsoftonline.com/";
            var organizationUrl = Environment.GetEnvironmentVariable("D365OrgUrl"); //"https://firstkeyhomestest.crm.dynamics.com";
            log.Info($"D365 Org Url : {organizationUrl}");
            var tenantId = "aa33e5f2-00dd-407e-b337-8cb00f28c25d";//[Azure AD Tenant ID];
            var clientId = Environment.GetEnvironmentVariable("ClientId"); //"df227c93-0513-43be-a02c-690167459b52";//[Azure AD Application ID];
            log.Info($"CLient ID : {clientId}");
            var appKey = Environment.GetEnvironmentVariable("appKey"); //"OXE+UZQfM6vIQPVwBD2B/KL4O56jfJ0BJHLsuXSSqgk=";//[Azure AD Application Key];
            log.Info($"Application Key : {appKey}");
            var clientcred = new ClientCredential(clientId, appKey);
            var authenticationContext = new AuthenticationContext(aadInstance + tenantId);
            var authenticationResult = authenticationContext.AcquireTokenAsync(organizationUrl, clientcred);
            var requestedToken = authenticationResult.Result.AccessToken;
            var sdkService = new OrganizationWebProxyClient(GetServiceUrl(organizationUrl), false);

            if (sdkService is OrganizationWebProxyClient)
            {
                sdkService.HeaderToken = requestedToken;
                OrganizationRequest request = new OrganizationRequest()
                {
                    RequestName = "WhoAmI"
                };
                WhoAmIResponse response = sdkService.Execute(new WhoAmIRequest()) as WhoAmIResponse;
                log.Info("D365 User ID : " + response.UserId);
                return sdkService;
            }
            else
                return null;
        }

        private static Uri GetServiceUrl(string organizationUrl)
        {
            return new Uri(
             organizationUrl +
             @"/xrmservices/2011/organization.svc/web?SdkClientVersion=8.2"
          );
        }

    }
}

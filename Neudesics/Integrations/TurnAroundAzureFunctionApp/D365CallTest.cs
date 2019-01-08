using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.WebServiceClient;
using Newtonsoft.Json;

namespace TurnAroundAzureFunctionApp
{
    public static class D365CallTest
    {
        [FunctionName("D365CallTest")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            var content = req.Content;
            const string CustomTopicEvent = "EventGridDoc-Topic";

            //var clientID = Environment.GetEnvironmentVariable("ClientId");
            //log.Info($"CLient ID : {clientID}");
            //var D365OrgUrl = Environment.GetEnvironmentVariable("D365OrgUrl");
            //log.Info($"D365 Org Url : {D365OrgUrl}");
            //var appKey = Environment.GetEnvironmentVariable("appKey");
            //log.Info($"Application Key : {appKey}");

            string jsonContent = await content.ReadAsStringAsync();
   /*         jsonContent = @"[{
  'Id': 'ee008e26-cc59-49bd-ba54-b6e2b8e39e7a',
  'EventType': 'filteredEvent',
  'Subject': 'Event 1',
  'EventTime': '2018-10-25T21:46:28.3015347Z',
  'Data': {
    'Property': '106',
    'Event': '60 Days Notification'
  },
  'dataVersion': '',
  'metadataVersion': '1',
  'Topic': '/subscriptions/bd07bd09-5b23-4701-9ddb-b8de9bf1802d/resourceGroups/D365Integration/providers/Microsoft.EventGrid/topics/EventGridDoc-Topic'
}]";*/
            log.Info($"Received Event with payload: {jsonContent}");
            CRMCall(log);
            /*
            EventGridSubscriber eventGridSubscriber = new EventGridSubscriber();
            eventGridSubscriber.AddOrUpdateCustomEventMapping(CustomTopicEvent, typeof(GridEvent<TurnAround>));

            EventGridEvent[] eventGridEvents = eventGridSubscriber.DeserializeEventGridEvents(jsonContent);

            foreach (EventGridEvent eventGridEvent in eventGridEvents)
            {
                if (eventGridEvent.Data is SubscriptionValidationEventData)
                {
                    var eventData = (SubscriptionValidationEventData)eventGridEvent.Data;
                    log.Info($"Got SubscriptionValidation event data, validationCode: {eventData.ValidationCode},  validationUrl: {eventData.ValidationUrl}, topic: {eventGridEvent.Topic}");
                    // Do any additional validation (as required) such as validating that the Azure resource ID of the topic matches
                    // the expected topic and then return back the below response
                    var responseData = new SubscriptionValidationResponse()
                    {
                        ValidationResponse = eventData.ValidationCode
                    };

                    return req.CreateResponse(HttpStatusCode.OK, responseData);
                }
                else if (eventGridEvent.Data is StorageBlobCreatedEventData)
                {
                    var eventData = (StorageBlobCreatedEventData)eventGridEvent.Data;
                    log.Info($"Got BlobCreated event data, blob URI {eventData.Url}");
                }
                else if (eventGridEvent.Data is object)
                {

                    TurnAround tr = JsonConvert.DeserializeObject<TurnAround>( eventGridEvent.Data.ToString());
                    if (tr is TurnAround)
                    {
                        OrganizationWebProxyClient _service = CRMCall(log);
                        if (_service is OrganizationWebProxyClient)
                        {
                            Entity azrIntegrationCallEntity = new Entity(Constants.AzureIntegrationCalls.LogicalName);
                            azrIntegrationCallEntity[Constants.AzureIntegrationCalls.EventName] = tr.Event.ToString();
                            azrIntegrationCallEntity[Constants.AzureIntegrationCalls.EventData] = jsonContent;

                            azrIntegrationCallEntity.Id = _service.Create(azrIntegrationCallEntity);
                            log.Info($"Event Successfully Posted to D365 with Integration Call ID : {azrIntegrationCallEntity.Id.ToString()}");
                        }
                    }
                    log.Info($"Got ContosoItemReceived event data, item Topic {eventGridEvent.Topic}");
                }
            }
            */
            return jsonContent == null
            ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
            : req.CreateResponse(HttpStatusCode.OK, "Hello " + jsonContent);
        }

        private static OrganizationWebProxyClient CRMCall(TraceWriter log)
        {
            var aadInstance = "https://login.microsoftonline.com/";
            //var organizationUrl = "https://firstkeyhomesdev.crm.dynamics.com";                //DEV ENVIRONMENT
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

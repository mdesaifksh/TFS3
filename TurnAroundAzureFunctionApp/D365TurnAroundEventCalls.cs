using System;
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
    public static class D365TurnAroundEventCalls
    {
        [FunctionName("D365TurnAroundEventCalls")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function)]HttpRequestMessage req, TraceWriter log)
        {
            var content = req.Content;
            const string CustomTopicEvent = "EventGridDoc-Topic";

            string jsonContent = await content.ReadAsStringAsync();

            log.Info($"Received Event with payload: {jsonContent}");
            //CRMCall(log);
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

                    TurnAround tr = JsonConvert.DeserializeObject<TurnAround>(eventGridEvent.Data.ToString());
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


            return jsonContent == null
            ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
            : req.CreateResponse(HttpStatusCode.OK, "Hello " + jsonContent);
        }

        private static OrganizationWebProxyClient CRMCall(TraceWriter log)
        {
            var aadInstance = "https://login.microsoftonline.com/";
            var organizationUrl = "https://firstkeyhomesdev.crm.dynamics.com";
            var tenantId = "aa33e5f2-00dd-407e-b337-8cb00f28c25d";//[Azure AD Tenant ID];
            var clientId = "df227c93-0513-43be-a02c-690167459b52";//[Azure AD Application ID];
            var appKey = "OXE+UZQfM6vIQPVwBD2B/KL4O56jfJ0BJHLsuXSSqgk=";//[Azure AD Application Key];
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

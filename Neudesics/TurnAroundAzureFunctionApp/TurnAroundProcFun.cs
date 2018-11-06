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
using Newtonsoft.Json;

namespace TurnAroundAzureFunctionApp
{
    public static class TurnAroundProcFun
    {
        [FunctionName("TurnAroundProcFun")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function)]HttpRequestMessage req, TraceWriter log)
        {

            log.Info("C# HTTP trigger function processed a request.");
            // parse query parameter
            var content = req.Content;

            string jsonContent = await content.ReadAsStringAsync();
            log.Info($"Received Event with payload: {jsonContent}");

            EventGridSubscriber eventGridSubscriber = new EventGridSubscriber();

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
            }


            return jsonContent == null
            ? req.CreateResponse(HttpStatusCode.BadRequest, "Pass a name on the query string or in the request body")
            : req.CreateResponse(HttpStatusCode.OK, "Hello " + jsonContent);
        }


    }



}

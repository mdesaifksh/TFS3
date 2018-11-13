using System.Linq;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Core;
using System.Collections.Generic;
using Microsoft.Azure.EventGrid.Models;

namespace SBEventGridIntegration
{
    public static class ReceiveMessagesOnEvent
    {
        const string ServiceBusConnectionString = "Endpoint=sb://fsk-d365-servicebus-eventgrid.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=DQS/OOCEsChq3TGAXva6KbdekFwe1zOQznGBbmiKo2w=";
        const int numberOfMessages = 10; // Choose the amount of messages you want to receive. Note that this is receive batch and there is no guarantee you will get all the messages.        
        static IMessageReceiver messageReceiver;

        [FunctionName("ReceiveMessagesOnEvent")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");
            // parse query parameter
            var content = req.Content;
            const string CustomTopicEvent = "Contoso.Items.ItemReceived";

            // Get content
            string jsonContent = await content.ReadAsStringAsync();
            log.Info($"Received Event with payload: {jsonContent}");

            EventGridSubscriber eventGridSubscriber = new EventGridSubscriber();
            eventGridSubscriber.AddOrUpdateCustomEventMapping(CustomTopicEvent, typeof(GridEvent));

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
                else if (eventGridEvent.Data is GridEvent)
                {
                    var eventData = (GridEvent)eventGridEvent.Data;
                    ReceiveAndProcess(log, eventData).GetAwaiter().GetResult();
                    log.Info($"Got ContosoItemReceived event data, item Topic {eventData.Topic}");
                }
            }

            /*
            IEnumerable<string> headerValues;
            if (req.Headers.TryGetValues("Aeg-Event-Type", out headerValues))
            {
                // Handle Subscription validation (Whenever you create a new subscription we send a new validation message)
                var validationHeaderValue = headerValues.FirstOrDefault();
                if (validationHeaderValue == "SubscriptionValidation")
                {
                    var events = JsonConvert.DeserializeObject<GridEvent[]>(jsonContent);
                    var code = events[0].Data["validationCode"];
                    return req.CreateResponse(HttpStatusCode.OK,
                    new { validationResponse = code });
                }
                // React to new messages and receive
                else
                {
                    ReceiveAndProcess(log, JsonConvert.DeserializeObject<GridEvent[]>(jsonContent)).GetAwaiter().GetResult();
                }
            }
            */
            return jsonContent == null
            ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
            : req.CreateResponse(HttpStatusCode.OK, "Hello " + jsonContent);
        }

        static async Task ReceiveAndProcess(TraceWriter log, GridEvent ge)
        {            
            log.Info($"TopicName: {ge.Data["topicName"]} : SubscriptionName: {ge.Data["subscriptionName"]}");
            // Get entity path, at this point you would in case you want to use Event Grid to monitor and react to deadletter messages likely also look for that.
            string EntityPath = $"{ge.Data["topicName"]}/subscriptions/{ge.Data["subscriptionName"]}";// e.g.: topicname/subscriptions/subscriptionname

            // Create MessageReceiver
            messageReceiver = new MessageReceiver(ServiceBusConnectionString, EntityPath, ReceiveMode.PeekLock, null, numberOfMessages);

            // Receive messages
            await ReceiveMessagesAsync(numberOfMessages, log);            
            await messageReceiver.CloseAsync();
        }

        static async Task ReceiveMessagesAsync(int numberOfMessagesToReceive, TraceWriter tw)
        {            
            // Receive the message
            IList<Message> receiveList = await messageReceiver.ReceiveAsync(numberOfMessagesToReceive);
            foreach (Message msg in receiveList)
            {
                tw.Info($"Received message: SequenceNumber:{msg.SystemProperties.SequenceNumber} Body:{Encoding.UTF8.GetString(msg.Body)}");
                await messageReceiver.CompleteAsync(msg.SystemProperties.LockToken);
            }
        }
    }

    public class GridEvent
    {
        public string Id { get; set; }
        public string EventType { get; set; }
        public string Subject { get; set; }
        public System.DateTime EventTime { get; set; }
        public Dictionary<string, string> Data { get; set; }
        public string Topic { get; set; }
    }
}

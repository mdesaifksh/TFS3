using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MessageSender
{
    class Program
    {
        const string ServiceBusConnectionString = "Endpoint=sb://fsk-d365-servicebus-eventgrid.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=DQS/OOCEsChq3TGAXva6KbdekFwe1zOQznGBbmiKo2w=";//"YOUR CONNECTION STRING";
        const string TopicName = "turnaroundsbtopic";//"YOUR TOPIC NAME";
        static ITopicClient TopicClient;

        static void Main(string[] args)
        {
            //MainAsync().GetAwaiter().GetResult();

            //Console.ReadKey();
            callTopicAsync().GetAwaiter().GetResult();
            Console.ReadKey();
        }

        static async Task callTopicAsync()
        {
            string topicEndpoint = "https://turnaroundeventgridtopic.eastus-1.eventgrid.azure.net/api/events";
            string sasKey = "v6xbKQuwLMjh7fus4KJ+4plVXDEQJlkuFswZAlzEdjM=";

            Console.WriteLine("================================================");
            Console.WriteLine("Press any key to exit after sending the message.");
            Console.WriteLine("================================================");

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("aeg-sas-key", sasKey);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("democlient");

            List<GridEvent<TurnAround>> eventList = new List<GridEvent<TurnAround>>();
            Random rnd = new Random();
            TurnAroundEvents events;
            events = TurnAroundEvents.Sixty_Days_Notice;
            for (int x = 0; x < 1; x++)
            {
                var propertyId = (x + 2) * rnd.Next(300, 500);
                TurnAround tr = new TurnAround();
                tr.Event = events;
                tr.PropertyID = $"{propertyId}";
                tr.Date1 = DateTime.Now;
                Console.WriteLine($"Property ID : {propertyId}.");

                GridEvent<TurnAround> testEvent = new GridEvent<TurnAround>
                {
                    Subject = $"{events.ToString()}",
                    EventType = (x % 2 == 0) ? "allEvents" : "filteredEvent",
                    EventTime = DateTime.UtcNow,
                    Data = tr,
                    Id = Guid.NewGuid().ToString(),
                    Topic = string.Empty//"EventGridDoc-Topic"

                };
                eventList.Add(testEvent);
            }

            string json = JsonConvert.SerializeObject(eventList);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, topicEndpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response = await client.SendAsync(request);
            Console.WriteLine("================================================");
            Console.WriteLine("Message successfully sent.");
            Console.WriteLine("================================================");
        }

        static async Task MainAsync()
        {
            const int numberOfMessages = 5;
            TopicClient = new TopicClient(ServiceBusConnectionString, TopicName);

            Console.WriteLine("================================================");
            Console.WriteLine("Press any key to exit after sending the message.");
            Console.WriteLine("================================================");            

            // Send Messages
            await SendMessagesAsync(numberOfMessages);

            Console.ReadKey();

            await TopicClient.CloseAsync();
        }               

        static async Task SendMessagesAsync(int numberOfMessagesToSend)
        {
            try
            {
                for (var i = 1; i <= numberOfMessagesToSend; i++)
                {
                    // Create a new message to send to the queue
                    string messageBody = $"Message {i} - {DateTime.Now}";
                    var message = new Message(Encoding.UTF8.GetBytes(messageBody));

                    // Write the body of the message to the console
                    Console.WriteLine($"Sending message: {messageBody}");

                    // Send the message to the queue
                    await TopicClient.SendAsync(message);
                    Thread.Sleep(5000);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
            }
        }

        static GridEvent<TurnAround> SixtyDayNotification()
        {
            GridEvent<TurnAround> sixtyDayNotification = new GridEvent<TurnAround>();
            sixtyDayNotification.EventTime = DateTime.Now;
            sixtyDayNotification.EventType = "allEvents";
            sixtyDayNotification.Id = Guid.NewGuid().ToString();
            sixtyDayNotification.Topic = string.Empty;

            sixtyDayNotification.Data = new TurnAround();
            sixtyDayNotification.Data.Event = TurnAroundEvents.Sixty_Days_Notice;
            sixtyDayNotification.Data.PropertyID = "1234";
            sixtyDayNotification.Data.Date1 = new DateTime(2018, 10, 20);   //Notification Date.
            sixtyDayNotification.Data.IsForce = false;      //To Perform Force Update in CRM. True = Perform Force Update.

            return sixtyDayNotification;
        }

        static GridEvent<TurnAround> FotoNotes_MoveOut_Inspection_Completed()
        {
            GridEvent<TurnAround> FotoNotes_MoveOut_Insp_Complete = new GridEvent<TurnAround>();
            FotoNotes_MoveOut_Insp_Complete.EventTime = DateTime.Now;
            FotoNotes_MoveOut_Insp_Complete.EventType = "allEvents";
            FotoNotes_MoveOut_Insp_Complete.Id = Guid.NewGuid().ToString();
            FotoNotes_MoveOut_Insp_Complete.Topic = string.Empty;

            FotoNotes_MoveOut_Insp_Complete.Data = new TurnAround();
            FotoNotes_MoveOut_Insp_Complete.Data.Event = TurnAroundEvents.FotoNotes_Move_Out_Insp_Complete;
            FotoNotes_MoveOut_Insp_Complete.Data.PropertyID = "1234";
            FotoNotes_MoveOut_Insp_Complete.Data.Date1 = new DateTime(2018, 10, 20);   //Inspection Start Date.
            FotoNotes_MoveOut_Insp_Complete.Data.Date2 = new DateTime(2018, 10, 31);   //Inspection Completion Date
            FotoNotes_MoveOut_Insp_Complete.Data.IsForce = false;      //To Perform Force Update in CRM. True = Perform Force Update.

            return FotoNotes_MoveOut_Insp_Complete;
        }

        static GridEvent<TurnAround> BudgetStarted()
        {
            GridEvent<TurnAround> budgetStarted = new GridEvent<TurnAround>();
            budgetStarted.EventTime = DateTime.Now;
            budgetStarted.EventType = "allEvents";
            budgetStarted.Id = Guid.NewGuid().ToString();
            budgetStarted.Topic = string.Empty;

            budgetStarted.Data = new TurnAround();
            budgetStarted.Data.Event = TurnAroundEvents.Renowalk_Budget_Started;
            budgetStarted.Data.PropertyID = "1234";
            budgetStarted.Data.Date1 = new DateTime(2018, 10, 20);   //Budget Start Date.
            budgetStarted.Data.IsForce = false;      //To Perform Force Update in CRM. True = Perform Force Update.

            return budgetStarted;
        }

    }

    public class GridEvent<T>
    {
        public string Id { get; set; }
        public string EventType { get; set; }
        public string Subject { get; set; }
        public DateTime EventTime { get; set; }
        public T Data { get; set; }
        public string Topic { get; set; }
    }

    public class TurnAround
    {
        public TurnAroundEvents Event { get; set; }
        public string PropertyID { get; set; }
        public DateTime Date1 { get; set; }
        public DateTime Date2 { get; set; }
        public bool IsForce { get; set; }
    }

    public enum TurnAroundEvents
    {
        Sixty_Days_Notice = 1,
        Yardi_Lease_Renewal_Received = 2,
        Thirty_Days_Notice = 3,
        Yardi_PreMove_Out_Scheduled = 4,
        Renowalk_Budget_Started = 5,
        FotoNotes_Move_Out_Insp_Complete = 6,
        Renowalk_Project_Status_Walked,
        Yardi_Jobs_Created,
        Jobs_Assigned_To_Vendor,
        Yardi_Jobs_Started,
        FotoNotes_Interim_Insp_Complete,
        Vendor_Request_Change_Order,
        Change_Order_Approved,
        FotoNotes_QC_Insp_Complete_With_Add_Work,
        FotoNotes_QC_Insp_Complete_With_No_Add_Work,
        FotoNotes_Marketing_Insp_Complete,
        FotoNotes_Bi_Weekly_Insp_Complete,
        FotoNotes_Move_In_Insp_Complete
    }
}

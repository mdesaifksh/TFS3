using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using FirstKey.D365.Plug_Ins;
using System.Text;
using System.Runtime.Serialization.Json;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using Microsoft.Xrm.Sdk.Query;
using System.Text.RegularExpressions;

namespace PlugInTest
{

    class Program
    {


        private static CrmServiceClient _client;
        static IOrganizationService _service;

        public static void Main(string[] args)
        {
            try
            {
                string str = @"1.1.1.1";

                str = str.RemoveAllButFirst(".");

                using (_client = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMConnectionString"].ConnectionString))
                {
                    _service = (IOrganizationService)_client.OrganizationWebProxyClient != null ? (IOrganizationService)_client.OrganizationWebProxyClient : (IOrganizationService)_client.OrganizationServiceProxy;



                    //Do stuff
                    WhoAmIResponse res = (WhoAmIResponse)_client.Execute(new WhoAmIRequest());
                    Console.WriteLine($"Login User ID : {res.UserId}");
                    Console.WriteLine($"Organization Unique Name : {_client.ConnectedOrgUniqueName}");
                    Console.WriteLine($"Organization Display Name : {_client.ConnectedOrgFriendlyName}");

                    string emailSubject = "Test Email From Malhar To Test Email Case Issue";
                    string TicketTitle = "INC-10637-2019 - Test Email From Malhar To Test Email Case Issue";

                    bool strcontains = TicketTitle.Contains(emailSubject);
                    //CreateOutGoingAzureIntegrationCallRecord();

                   
                    CrmContext con = new CrmContext();
                    ITracingService tracingService = new NullCrmTracingService();

                    
                   XmlSerializer serializer = new XmlSerializer(typeof(ProjectTemplateSettings));

                   ProjectTemplateSettings settings;
                   using (var fs = new FileStream("AzureIntegrationCallAsyncSettings.xml", FileMode.Open))
                   {
                       // Uses the Deserialize method to restore the object's state   
                       // with data from the XML document. 
                       settings = (ProjectTemplateSettings)serializer.Deserialize(fs);
                   }
                    /*
                    //CommonMethods.ChangeEntityStatus(tracingService, _service, new EntityReference(Constants.Projects.LogicalName, new Guid("556C6EE4-E8E2-E811-A976-000D3A1A42B9")),1,2);

                    //Entity test = RetrieveProjectTemplateTask(new EntityReference("msdyn_project", new Guid("23A38E60-C0D0-E811-A96E-000D3A16ACEE")), "1");
                     */
                    //Entity azureIntegrationCallEntity = _client.Retrieve(Constants.AzureIntegrationCalls.LogicalName, new Guid("04D177A1-323D-E911-A953-000D3A1D5D5A"), new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

                    //con.InputParameters[Constants.TARGET] = azureIntegrationCallEntity;
                    //List<GridEvent<DataPayLoad>> GridEventList = CommonMethods.Deserialize<List<GridEvent<DataPayLoad>>>(azureIntegrationCallEntity.GetAttributeValue<string>("fkh_eventdata").Replace('\'','"'));
                    //foreach (GridEvent<DataPayLoad> turnAround in GridEventList)
                    //{
                    //    DateTime eventDate;
                    //    DateTime.TryParse(turnAround.EventTime, out eventDate);
                    //}

                    //AzureIntegrationCallAsync.ProcessIncomingIntegrationCall(tracingService, _service, azureIntegrationCallEntity, settings);

                    CalculateTurnSchStartandEndDate();



                    //DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(GridEvent<TurnAround>));
                    //var m = new MemoryStream(Encoding.UTF8.GetBytes(azureIntegrationCallEntity.GetAttributeValue<string>("fkh_eventdata").Replace("\r\n", "").Replace("[", "").Replace("]", "")));
                    //var cl1 = ser.ReadObject(m) as GridEvent<TurnAround>;


                    Console.WriteLine($"Press any key to exit.");
                    Console.Read();

                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                string message = ex.Message;
                throw;
            }


        }

        private static void CalculateTurnSchStartandEndDate()
        {
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.JobVendors.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.JobVendors.JobID,
                            Operator = ConditionOperator.Equal,
                            Values = { new Guid("257FCCE2-3121-E911-A952-000D3A1D52E7") }
                        }
                    }
                },
                TopCount = 1000,
                Orders = { new OrderExpression(Constants.JobVendors.StartDate, OrderType.Ascending) }
            };

            EntityCollection jobVendorsEntityCollection = _service.RetrieveMultiple(Query);

            Entity startDateEntity = jobVendorsEntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.JobVendors.StartDate)).OrderBy(e => e.Attributes[Constants.JobVendors.StartDate]).FirstOrDefault();
            Entity endDateEntity = jobVendorsEntityCollection.Entities.Where(e => e.Attributes.Contains(Constants.JobVendors.EndDate)).OrderByDescending(e => e.Attributes[Constants.JobVendors.EndDate]).FirstOrDefault();

        }

        private static void CreateOutGoingAzureIntegrationCallRecord()
        {
            List<GridEvent<DataPayLoad>> gridEventDataPayloadList = new List<GridEvent<DataPayLoad>>();
            GridEvent<DataPayLoad> gridEventDataPayload = new GridEvent<DataPayLoad>();
            gridEventDataPayload.EventTime = DateTime.Now.ToString();
            gridEventDataPayload.EventType = "allEvents";
            gridEventDataPayload.Id = Guid.NewGuid().ToString();
            gridEventDataPayload.Subject = $"Turn Process : {Events.JOB_COMPLETED.ToString()}";
            gridEventDataPayload.data = new DataPayLoad();
            gridEventDataPayload.data.Date1 = DateTime.Now.ToString();
            gridEventDataPayload.data.Event = Events.JOB_COMPLETED;
            gridEventDataPayload.data.IsForce = false;
            gridEventDataPayload.data.PropertyID = "10139104";
            gridEventDataPayload.Topic = string.Empty;

            gridEventDataPayloadList.Add(gridEventDataPayload);


            Entity azIntCallEntity = new Entity(Constants.AzureIntegrationCalls.LogicalName);
            azIntCallEntity[Constants.AzureIntegrationCalls.EventData] = CommonMethods.Serialize(gridEventDataPayloadList);
            azIntCallEntity[Constants.AzureIntegrationCalls.Direction] = true;
            azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.JOB_COMPLETED.ToString();

            _service.Create(azIntCallEntity);
        }

        

        //[DataContract]
        //public class GridEvent<T>
        //{
        //    [DataMember(Name = "id")]
        //    public string Id { get; set; }
        //    [DataMember(Name = "eventType")]
        //    public string EventType { get; set; }
        //    [DataMember(Name = "subject")]
        //    public string Subject { get; set; }
        //    [DataMember(Name = "eventTime")]
        //    public string EventTime { get; set; }
        //    [DataMember(Name = "data")]
        //    public T data { get; set; }
        //    [DataMember(Name = "topic")]
        //    public string Topic { get; set; }
        //}

        //[DataContract]
        //public class TurnAround
        //{
        //    [DataMember]
        //    public int Event { get; set; }
        //    [DataMember]
        //    public string PropertyID { get; set; }
        //    [DataMember]
        //    public string Date1 { get; set; }
        //    [DataMember]
        //    public string Date2 { get; set; }
        //    [DataMember]
        //    public bool IsForce { get; set; }
        //}
        

    }
}
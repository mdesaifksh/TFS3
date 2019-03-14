using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace ChangeUserAccessMode
{
    class Program
    {
        private static CrmServiceClient _client;
        static IOrganizationService _service;
        const int READ_WRITE_ACCESS_MODE = 0;
        const int ADMINISTRATIVE_ACCESS_MODE = 1;
        static string[] excludeUsers;


        public static void Main(string[] args)
        {
            try
            {
                using (_client = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMConnectionString"].ConnectionString))
                {
                    //Do stuff
                    _service = (IOrganizationService)_client.OrganizationWebProxyClient != null ? (IOrganizationService)_client.OrganizationWebProxyClient : (IOrganizationService)_client.OrganizationServiceProxy;

                    WhoAmIResponse res = (WhoAmIResponse)_client.Execute(new WhoAmIRequest());

                    Console.WriteLine($"Login User ID : {res.UserId}");
                    Console.WriteLine($"Organization Unique Name : {_client.ConnectedOrgUniqueName}");
                    Console.WriteLine($"Organization Display Name : {_client.ConnectedOrgFriendlyName}");

                    if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ExcludeUsers"]))
                        excludeUsers = ConfigurationManager.AppSettings["ExcludeUsers"].Split(',');

                    ChangeAllUserAccessMode(READ_WRITE_ACCESS_MODE);
                    Console.Read();

                }
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                string message = ex.Message;
                throw;
            }
        }

        private static void ChangeAllUserAccessMode(int accessmode)
        {


            QueryExpression queryExpression = new QueryExpression();
            queryExpression.EntityName = "systemuser";
            queryExpression.ColumnSet = new ColumnSet("fullname", "domainname");
            //queryExpression.ColumnSet.AddColumn();

            if (excludeUsers != null && excludeUsers.Length > 0)
            {
                ConditionExpression conditionExpression1 = new ConditionExpression();
                conditionExpression1.AttributeName = "domainname";
                foreach (string st in excludeUsers)
                {
                    Console.WriteLine($"Excluded User Email : {st}");
                    conditionExpression1.Values.Add(st);
                }
                conditionExpression1.Operator = ConditionOperator.NotIn;
                queryExpression.Criteria.AddCondition(conditionExpression1);
            }

            ConditionExpression conditionExpression2 = new ConditionExpression();
            conditionExpression2.AttributeName = "isdisabled";
            conditionExpression2.Values.Add(false);
            conditionExpression2.Operator = ConditionOperator.Equal;
            queryExpression.Criteria.AddCondition(conditionExpression2);


            // access mode ==> 0 --> Read Write and 1 --> Adminstrative
            ConditionExpression conditionExpression3 = new ConditionExpression();
            conditionExpression3.AttributeName = "accessmode";
            conditionExpression3.Values.Add((accessmode == ADMINISTRATIVE_ACCESS_MODE) ? READ_WRITE_ACCESS_MODE : ADMINISTRATIVE_ACCESS_MODE);
            if(accessmode == ADMINISTRATIVE_ACCESS_MODE)
                Console.WriteLine($"Changing user access mode to ADMINISTRATIVE.");
            else
                Console.WriteLine($"Chaning User Access Mode to READ-WRITE.");

            conditionExpression3.Operator = ConditionOperator.Equal;
            queryExpression.Criteria.AddCondition(conditionExpression3);

            queryExpression.Criteria.FilterOperator = LogicalOperator.And;

            EntityCollection entityColl = _service.RetrieveMultiple(queryExpression);
            foreach (var entity in entityColl.Entities)
            {
                try
                {
                    Entity userEntity = new Entity("systemuser");
                    userEntity.Id = entity.Id;
                    userEntity.Attributes["accessmode"] = new OptionSetValue(accessmode);
                    _service.Update(userEntity);
                    Console.WriteLine($"User with domain name  : {entity.GetAttributeValue<string>("domainname") } successfully updated.");
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Error while updating User : {entity.GetAttributeValue<string>("domainname") }. Error : {ex.Message}");

                }
            }
        }
    }
}
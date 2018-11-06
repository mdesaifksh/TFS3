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

namespace D365Test
{
    class Program
    {
        private static CrmServiceClient _client;

        public static void Main(string[] args)
        {
            try
            {
                using (_client = new CrmServiceClient(ConfigurationManager.ConnectionStrings["CRMConnectionString"].ConnectionString))
                {
                    //Do stuff
                    WhoAmIResponse res = (WhoAmIResponse)_client.Execute(new WhoAmIRequest());
                    Console.WriteLine($"Login User ID : {res.UserId}");
                    Console.WriteLine($"Organization Unique Name : {_client.ConnectedOrgUniqueName}");
                    Console.WriteLine($"Organization Display Name : {_client.ConnectedOrgFriendlyName}");
                    //Entity test = RetrieveProjectTemplateTask(new EntityReference("msdyn_project", new Guid("23A38E60-C0D0-E811-A96E-000D3A16ACEE")), "1");

                    Entity ProjectEntity = new Entity("msdyn_project");
                    ProjectEntity["fkh_unitid"] = new EntityReference("po_unit", new Guid("6068F309-8A9D-E811-A857-000D3A14019B"));
                    ProjectEntity["msdyn_projecttemplate"] = new EntityReference("msdyn_project", new Guid("23A38E60-C0D0-E811-A96E-000D3A16ACEE"));
                    ProjectEntity["msdyn_subject"] = "Malhar Test Project 2";

                    ProjectEntity.Id = _client.Create(ProjectEntity);


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

        private static Entity RetrieveProjectTemplateTask(EntityReference projectTemplateEntityReference, string WBSID)
        {
            Console.WriteLine($"Retrieve Project Template Task with WBSID : {WBSID}.");

            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.ProjectTasks.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTasks.WBSID,
                            Operator = ConditionOperator.Equal,
                            Values = { WBSID }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTasks.TaskIdentifier,
                            Operator = ConditionOperator.NotNull
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTasks.Project,
                            Operator = ConditionOperator.Equal,
                            Values = { projectTemplateEntityReference.Id }
                        }
                    }
                },
                TopCount = 1
            };

            EntityCollection projectTaskEntityCollection = _client.RetrieveMultiple(Query);
            if (projectTaskEntityCollection.Entities.Count > 0)
            {
                Console.WriteLine($"Project Template Task found.");
                return projectTaskEntityCollection.Entities[0];
            }
            else
            {
                Console.WriteLine($"Project Template Task NOT found.");
                return null;
            }
        }

    }
}
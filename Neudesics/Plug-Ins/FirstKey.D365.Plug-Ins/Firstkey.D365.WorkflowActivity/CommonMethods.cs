using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Firstkey.D365.WorkflowActivity
{
    public class CommonMethods
    {

        private const string activeProjectCountFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'  aggregate='true'>
                                              <entity name='msdyn_project'>
                                                <attribute name='msdyn_projectid'  alias='totalcount' aggregate='count'/>
                                                <filter type='and'>
                                                  <condition attribute='fkh_unitid' operator='eq' value='{0}' />
                                                  <condition attribute='statecode' operator='eq' value='0' />
                                                </filter>
                                              </entity>
                                            </fetch>";

        private const string projectTaskCountFetchXml = @"<fetch version='1.0' output-format='xml-platform' mapping='logical' distinct='false'  aggregate='true'>
                                              <entity name='msdyn_projecttask'>
                                                <attribute name='msdyn_projecttaskid'  alias='totalcount' aggregate='count'/>
                                                <filter type='and'>
                                                  <condition attribute='msdyn_project' operator='eq' value='{0}' />
                                                </filter>
                                              </entity>
                                            </fetch>";
        #region JSON Converter

        /// <summary>
        /// Json Deserialize using .NET Framework 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T Deserialize<T>(string json)
        {
            var instance = Activator.CreateInstance<T>();
            using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(instance.GetType());
                return (T)serializer.ReadObject(ms);
            }
        }


        /// <summary>
        /// Json Serialize using .NET Framework
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static string Serialize<T>(T entity)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
                ser.WriteObject(ms, entity);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static int CountActiveProjectForUnit(ITracingService tracer,IOrganizationService service, EntityReference unitEntityReference)
        {
            EntityCollection activeCount = service.RetrieveMultiple(new FetchExpression(string.Format(activeProjectCountFetchXml, unitEntityReference.Id.ToString())));

            foreach (var c in activeCount.Entities)
            {
                return (int)((AliasedValue)c["totalcount"]).Value;

            }
            return 0;

        }
        #endregion

        public static int CountprojectTaskForProject(ITracingService tracer, IOrganizationService service, EntityReference projectEntityReference)
        {
            EntityCollection activeCount = service.RetrieveMultiple(new FetchExpression(string.Format(projectTaskCountFetchXml, projectEntityReference.Id.ToString())));

            foreach (var c in activeCount.Entities)
            {
                return (int)((AliasedValue)c["totalcount"]).Value;

            }
            return 0;

        }

        public static EntityCollection RetrieveActivtProjectByUnitId(ITracingService tracer, IOrganizationService service, EntityReference unitEntityReference)
        {
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.Projects.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.Projects.Unit,
                            Operator = ConditionOperator.Equal,
                            Values = { unitEntityReference.Id }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.Status.StateCode,
                            Operator = ConditionOperator.Equal,
                            Values = { 0 }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.Projects.ProjectTemplate,
                            Operator = ConditionOperator.NotNull
                        }
                    }
                },
                TopCount = 100
            };

            return service.RetrieveMultiple(Query);
        }

        public static Entity RetrieveUnitByUnitId(ITracingService tracer,IOrganizationService service, string unitID)
        {
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.Units.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.Units.UnitId,
                            Operator = ConditionOperator.Equal,
                            Values = { unitID }
                        }
                    }
                },
                TopCount = 1
            };

            EntityCollection unitEntityCollection = service.RetrieveMultiple(Query);
            if (unitEntityCollection.Entities.Count > 0)
                return unitEntityCollection.Entities[0];
            else
                return null;
        }

        public static void ChangeEntityStatus(ITracingService tracer,IOrganizationService service, EntityReference entityReference, int Status, int StatusCode)
        {
            tracer.Trace($"ChangeEntityStatus Method Call.");
            try
            {
                SetStateRequest request = new SetStateRequest
                {
                    EntityMoniker = entityReference,
                    State = new OptionSetValue(Status),
                    Status = new OptionSetValue(StatusCode) 
                };
                service.Execute(request);
            }
            catch(Exception ex)
            {
                tracer.Trace($"ChangeEntityStatus Method Call Failed with Error {ex.Message}.{Environment.NewLine} {ex.StackTrace}.");

            }
        }

        public static Entity CreateProjectFromProjectTemplate(ITracingService tracer, IOrganizationService service, Entity unitEntity, Entity projectTemplateEntity, DateTime startDate)
        {
            tracer.Trace($"Creating {projectTemplateEntity.GetAttributeValue<string>(Constants.Projects.Subject)} for {unitEntity.GetAttributeValue<string>(Constants.Units.Name)}.");

            Entity projectEntity = new Entity(Constants.Projects.LogicalName);
            projectEntity[Constants.Projects.Unit] = unitEntity.ToEntityReference();
            projectEntity[Constants.Projects.ProjectTemplate] = projectTemplateEntity.ToEntityReference();
            projectEntity[Constants.Projects.Subject] = $"{projectTemplateEntity.GetAttributeValue<string>(Constants.Projects.Subject)} for {unitEntity.GetAttributeValue<string>(Constants.Units.Name)}";
            projectEntity[Constants.Projects.StartDate] = startDate;

            projectEntity.Id = service.Create(projectEntity);

            tracer.Trace($"{projectTemplateEntity.GetAttributeValue<string>(Constants.Projects.Subject)} for {unitEntity.GetAttributeValue<string>(Constants.Units.Name)} successfully created.");

            return projectEntity;
        }

        public static List<Entity> RetrieveActivtTaskIdentifier(ITracingService tracer, IOrganizationService service)
        {
            QueryExpression queryExpression = new QueryExpression()
            {
                EntityName = Constants.TaskIdentifiers.LogicalName,
                ColumnSet = new ColumnSet(true)
            };
            FilterExpression filterExpression = new FilterExpression()
            {
                FilterOperator = LogicalOperator.And
            };
            DataCollection<ConditionExpression> conditions = filterExpression.Conditions;
            ConditionExpression conditionExpression = new ConditionExpression()
            {
                AttributeName = Constants.Status.StateCode,
                Operator = ConditionOperator.Equal
            };
            conditionExpression.Values.Add(0);
            conditions.Add(conditionExpression);
            filterExpression.Conditions.Add(new ConditionExpression()
            {
                AttributeName = Constants.TaskIdentifiers.IdentifierNumber,
                Operator = ConditionOperator.NotNull
            });
            queryExpression.Criteria = filterExpression;
            queryExpression.TopCount = new int?(100);
            return service.RetrieveMultiple(queryExpression).Entities.ToList<Entity>();
        }

        public static EntityCollection RetrieveProjectTaskByProjectAndTaskIdentifier(ITracingService tracer, IOrganizationService service, EntityReference projectEntityRefernce, EntityReference projectTemplateEntityReference, int eventID, string WBSId = null)
        {
            QueryExpression queryExpression = new QueryExpression(Constants.ProjectTasks.LogicalName);
            queryExpression.ColumnSet = new ColumnSet(true);
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.Project, ConditionOperator.Equal, projectEntityRefernce.Id));

            LinkEntity taskIdentiferLinkEntity = new LinkEntity(Constants.ProjectTasks.LogicalName, Constants.TaskIdentifiers.LogicalName, Constants.ProjectTasks.TaskIdentifier, Constants.TaskIdentifiers.PrimaryKey, JoinOperator.Inner);
            taskIdentiferLinkEntity.Columns = new ColumnSet(true);
            taskIdentiferLinkEntity.EntityAlias = "TI";
            taskIdentiferLinkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.IdentifierNumber, ConditionOperator.Equal, eventID));
            if(!string.IsNullOrEmpty(WBSId))
                taskIdentiferLinkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.WBSID, ConditionOperator.Equal, WBSId));
            if (projectTemplateEntityReference is EntityReference)
                taskIdentiferLinkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.ProjectTemplateId, ConditionOperator.Equal, projectTemplateEntityReference.Id));
            queryExpression.LinkEntities.Add(taskIdentiferLinkEntity);

            queryExpression.TopCount = 10;

            return service.RetrieveMultiple(queryExpression);
        }

        public static DateTime RetrieveLocalTimeFromUTCTime(IOrganizationService service, int timeZoneCode, DateTime utcTime)
        {

            var request = new LocalTimeFromUtcTimeRequest
            {
                TimeZoneCode = timeZoneCode,
                UtcTime = utcTime.ToUniversalTime()
            };

            var response = (LocalTimeFromUtcTimeResponse)service.Execute(request);

            //Console.WriteLine(String.Concat("Calling LocalTimeFromUtcTimeRequest.  UTC time: ", utcTime.ToString("MM/dd/yyyy HH:mm:ss"), ". Local time: ", response.LocalTime.ToString("MM/dd/yyyy HH:mm:ss")));
            return response.LocalTime;
        }

        public static int RetrieveCurrentUsersSettings(IOrganizationService _service)
        {
            var currentUserSettingsEntityCollection = _service.RetrieveMultiple(
                new QueryExpression("usersettings")
                {
                    ColumnSet = new ColumnSet("timezonecode"),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("systemuserid", ConditionOperator.EqualUserId)
                        }
                    }
                });
            if (currentUserSettingsEntityCollection is EntityCollection && currentUserSettingsEntityCollection.Entities.Count > 0)
            {
                if (currentUserSettingsEntityCollection.Entities[0].Attributes.Contains("timezonecode"))
                    return currentUserSettingsEntityCollection.Entities[0].GetAttributeValue<int>("timezonecode");
                else
                    return -1;
            }

            return -1;

        }

    }

}

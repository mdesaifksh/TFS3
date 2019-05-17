using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;

namespace FirstKey.D365.Plug_Ins
{


    public static class Extensions
    {
        public static DateTime ChangeTime(this DateTime dateTime, int hours, int minutes, int seconds, int milliseconds)
        {
            return new DateTime(
                dateTime.Year,
                dateTime.Month,
                dateTime.Day,
                hours,
                minutes,
                seconds,
                milliseconds,
                dateTime.Kind);
        }

        public static string RemoveAllButFirst(this string s, string stuffToRemove)
        {
            // Check if the stuff to replace exists and if not, return the 
            // original string
            var locationOfStuff = s.IndexOf(stuffToRemove);
            if (locationOfStuff < 0)
            {
                return s;
            }
            // Calculate where to pull the first string from and then replace the rest of the string
            var splitLocation = locationOfStuff + stuffToRemove.Length;
            return s.Substring(0, splitLocation) + (s.Substring(splitLocation)).Replace(stuffToRemove, "");
        }
    }

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

        public static int CountActiveProjectForUnit(ITracingService tracer, IOrganizationService service, EntityReference unitEntityReference)
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

        public static EntityCollection RetrieveActivtProjectByRenowalkId(ITracingService tracer, IOrganizationService service, string renowalkID)
        {
            try
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
                            AttributeName = Constants.Projects.RenowalkID,
                            Operator = ConditionOperator.Equal,
                            Values = { renowalkID }
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
            catch(Exception Ex)
            {
                tracer.Trace($"Error in RetrieveActivtProjectByRenowalkId. Error Message : {Ex.Message}. Error Trace : {Ex.StackTrace}");
                return new EntityCollection();
            }
        }

        public static EntityCollection RetrieveJobsByRenowalkId(ITracingService tracer, IOrganizationService service, string renowalkID)
        {
            try
            {
                QueryExpression Query = new QueryExpression
                {
                    EntityName = Constants.Jobs.LogicalName,
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.Jobs.RenowalkID,
                            Operator = ConditionOperator.Equal,
                            Values = { renowalkID }
                        }
                    }
                    },
                    TopCount = 100
                };

                return service.RetrieveMultiple(Query);
            }
            catch (Exception Ex)
            {
                tracer.Trace($"Error in RetrieveJobsByRenowalkId. Error Message : {Ex.Message}. Error Trace : {Ex.StackTrace}");
                return new EntityCollection();
            }
        }

        public static Entity RetrieveUnitByUnitId(ITracingService tracer, IOrganizationService service, string unitID)
        {
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.Units.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.Or,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.Units.UnitId,
                            Operator = ConditionOperator.Equal,
                            Values = { unitID }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.Units.SFCode,
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

        public static void ChangeEntityStatus(ITracingService tracer, IOrganizationService service, EntityReference entityReference, int Status, int StatusCode)
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
            catch (Exception ex)
            {
                tracer.Trace($"ChangeEntityStatus Method Call Failed with Error {ex.Message}.{Environment.NewLine} {ex.StackTrace}.");

            }
        }


        public static Entity CreateProjectFromProjectTemplate(ITracingService tracer, IOrganizationService service, Entity unitEntity, Entity projectTemplateEntity, DateTime startDate, int timeZoneCode, GridEvent<DataPayLoad> gridEvent)
        {
            DateTime moveOutDate;
            startDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, startDate);
            tracer.Trace($"Local Date 1 : {startDate}");

            tracer.Trace($"Creating {projectTemplateEntity.GetAttributeValue<string>(Constants.Projects.Subject)} for {unitEntity.GetAttributeValue<string>(Constants.Units.Name)}.");
            string projectName = (projectTemplateEntity.GetAttributeValue<string>(Constants.Projects.Subject).Equals("Turn Process", StringComparison.OrdinalIgnoreCase) ? "Turn " : "Reno ") + unitEntity.GetAttributeValue<string>(Constants.Units.Name);
            Entity projectEntity = new Entity(Constants.Projects.LogicalName);
            projectEntity[Constants.Projects.Unit] = unitEntity.ToEntityReference();
            projectEntity[Constants.Projects.ProjectTemplate] = projectTemplateEntity.ToEntityReference();
            projectEntity[Constants.Projects.Subject] = projectName;
            projectEntity[Constants.Projects.StartDate] = startDate;
            projectEntity[Constants.Projects.ActualStartDate] = startDate;
            projectEntity[Constants.Projects.InitialJSONDataPayLoad] = CommonMethods.Serialize<DataPayLoad>(gridEvent.data);
            if (!string.IsNullOrEmpty(gridEvent.data.RenowalkID))
                projectEntity[Constants.Projects.RenowalkID] = gridEvent.data.RenowalkID;
            if (!DateTime.TryParse(gridEvent.data.Date2, out moveOutDate))
            {
                if (unitEntity.Attributes.Contains(Constants.Units.MoveOutDate))
                {
                    moveOutDate = unitEntity.GetAttributeValue<DateTime>(Constants.Units.MoveOutDate);
                }
            }
            moveOutDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, unitEntity.GetAttributeValue<DateTime>(Constants.Units.MoveOutDate));
            tracer.Trace($"Move Out Date : {moveOutDate}.");

            if (projectTemplateEntity.GetAttributeValue<string>(Constants.Projects.Subject).Equals("Turn Process", StringComparison.OrdinalIgnoreCase))
            {
                projectEntity[Constants.Projects.CurrentResidentMoveOutDate] = moveOutDate;
                tracer.Trace($"Est. Job Start Date : {moveOutDate.ChangeTime(6, 0, 0, 0).AddDays(1)}.");
                projectEntity[Constants.Projects.ScheduledJobStartDate] = moveOutDate.ChangeTime(6,0,0,0).AddDays(1);
                tracer.Trace($"Est. Job Complete Date : {moveOutDate.ChangeTime(6, 0, 0, 0).AddDays(6)}.");
                projectEntity[Constants.Projects.ScheduledJobCompletionDate] = moveOutDate.ChangeTime(6, 0, 0, 0).AddDays(6);
            }
            else
            {
                if (unitEntity.Attributes.Contains(Constants.Units.ScheduledAcquisitionDate)){
                    DateTime schStartDate = CommonMethods.RetrieveLocalTimeFromUTCTime(service, timeZoneCode, unitEntity.GetAttributeValue<DateTime>(Constants.Units.ScheduledAcquisitionDate));
                    projectEntity[Constants.Projects.ScheduledJobStartDate] = schStartDate.AddDays(1);
                    projectEntity[Constants.Projects.ScheduledJobCompletionDate] = schStartDate.AddDays(11);
                }
            }
            if (moveOutDate > startDate)
                projectEntity[Constants.Projects.DueDate] = moveOutDate.AddHours(24);

            projectEntity.Id = service.Create(projectEntity);

            tracer.Trace($"{projectTemplateEntity.GetAttributeValue<string>(Constants.Projects.Subject)} for {unitEntity.GetAttributeValue<string>(Constants.Units.Name)} successfully created.");
            SetUnitSyncJobFlag(tracer, service, unitEntity.ToEntityReference(), true);

            return projectEntity;
        }

        public static void SetUnitSyncJobFlag(ITracingService tracer, IOrganizationService service, EntityReference unitEntityReference, bool isActive)
        {
            tracer.Trace($"Setting Sync Job Flag for Unit Record.");

            Entity unitEntity = new Entity(unitEntityReference.LogicalName);
            unitEntity.Id = unitEntityReference.Id;
            unitEntity[Constants.Units.UnitSyncToMobile] = isActive;

            service.Update(unitEntity);
            tracer.Trace($"Sync Job Flag for Unit Record successfully set.");

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

        public static EntityCollection RetrieveAllProjectTaskByProjectAndTaskIdentifier(ITracingService tracer, IOrganizationService service, EntityReference projectEntityRefernce, EntityReference projectTemplateEntityReference, int TaskIdentifier, bool isChildTask = false)
        {
            QueryExpression queryExpression = new QueryExpression(Constants.ProjectTasks.LogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.Project, ConditionOperator.Equal, projectEntityRefernce.Id));
            if (!isChildTask)
            {
                queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.ParentTask, ConditionOperator.Null));
            }
            else
            {
                queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.ParentTask, ConditionOperator.NotNull));
            }
            LinkEntity linkEntity = new LinkEntity(Constants.ProjectTasks.LogicalName, Constants.TaskIdentifiers.LogicalName, Constants.ProjectTasks.TaskIdentifier, Constants.TaskIdentifiers.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(true),
                EntityAlias = "TI"
            };
            linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.IdentifierNumber, ConditionOperator.Equal, TaskIdentifier));
            if (projectTemplateEntityReference != null)
            {
                linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.ProjectTemplateId, ConditionOperator.Equal, projectTemplateEntityReference.Id));
            }
            queryExpression.LinkEntities.Add(linkEntity);
            queryExpression.TopCount = new int?(10);
            return service.RetrieveMultiple(queryExpression);
        }

        public static Entity CloneEntitySandbox(Entity entityToClone)
        {
            var newEntity = new Entity(entityToClone.LogicalName);
            var systemAttributes = new List<string>();
            systemAttributes.Add("createdon");
            systemAttributes.Add("createdby");
            systemAttributes.Add("modifiedon");
            systemAttributes.Add("modifiedby");
            systemAttributes.Add("owninguser");
            systemAttributes.Add("owningbusinessunit");


            foreach (var attribute in entityToClone.Attributes
                .Where(x => x.Key != entityToClone.LogicalName + "id")
                .Where(x => !systemAttributes.Contains(x.Key)))
            {

                switch (attribute.Value.GetType().Name)
                {
                    case "Money":
                        var m = attribute.Value as Money;
                        newEntity[attribute.Key] = new Money(m.Value);
                        break;
                    case "EntityReference":
                        var er = attribute.Value as EntityReference;
                        newEntity[attribute.Key] = new EntityReference(er.LogicalName, er.Id);
                        break;
                    case "OptionSetValue":
                        var os = attribute.Value as OptionSetValue;
                        newEntity[attribute.Key] = new OptionSetValue(os.Value);
                        break;
                    default:
                        newEntity[attribute.Key] = attribute.Value;
                        break;
                }

            }

            return newEntity;
        }


        public static EntityCollection RetrieveAllOpenProjectTaskByProject(ITracingService tracer, IOrganizationService service, EntityReference projectEntityRefernce, bool isChildTask = false)
        {
            QueryExpression queryExpression = new QueryExpression(Constants.ProjectTasks.LogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.Project, ConditionOperator.Equal, projectEntityRefernce.Id));
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.Status.StatusCode, ConditionOperator.In, new int[] { 1, 963850000 }));
            if (!isChildTask)
                queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.ParentTask, ConditionOperator.Null));
            else
                queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.ParentTask, ConditionOperator.NotNull));

            queryExpression.TopCount = 100;
            return service.RetrieveMultiple(queryExpression);
        }
        public static EntityCollection RetrieveOpenProjectTaskByProjectAndTaskIdentifier(ITracingService tracer, IOrganizationService service, EntityReference projectEntityRefernce, EntityReference projectTemplateEntityReference, int TaskIdentifier, string WBSId = null, bool isChildTask = false)
        {
            QueryExpression queryExpression = new QueryExpression(Constants.ProjectTasks.LogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.Project, ConditionOperator.Equal, projectEntityRefernce.Id));
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.Status.StatusCode, ConditionOperator.In, new int[] { 1, 963850000 }));
            if (!isChildTask)
            {
                queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.ParentTask, ConditionOperator.Null));
            }
            else
            {
                queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.ParentTask, ConditionOperator.NotNull));
            }
            LinkEntity linkEntity = new LinkEntity(Constants.ProjectTasks.LogicalName, Constants.TaskIdentifiers.LogicalName, Constants.ProjectTasks.TaskIdentifier, Constants.TaskIdentifiers.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(true),
                EntityAlias = "TI"
            };
            if (TaskIdentifier > 0)
                linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.IdentifierNumber, ConditionOperator.Equal, TaskIdentifier));
            if (!string.IsNullOrEmpty(WBSId))
                linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.WBSID, ConditionOperator.Equal, WBSId));
            if (projectTemplateEntityReference != null)
            {
                linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.ProjectTemplateId, ConditionOperator.Equal, projectTemplateEntityReference.Id));
            }
            queryExpression.LinkEntities.Add(linkEntity);
            queryExpression.TopCount = new int?(10);
            return service.RetrieveMultiple(queryExpression);
        }

        public static EntityCollection RetrieveAllProjectTaskIncludingChildByProjectAndTaskIdentifier(ITracingService tracer, IOrganizationService service, EntityReference projectEntityRefernce, EntityReference projectTemplateEntityReference, int TaskIdentifier, string WBSId = null)
        {
            QueryExpression queryExpression = new QueryExpression(Constants.ProjectTasks.LogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.ProjectTasks.Project, ConditionOperator.Equal, projectEntityRefernce.Id));
            queryExpression.Criteria.AddCondition(new ConditionExpression(Constants.Status.StatusCode, ConditionOperator.In, new int[] { 1, 963850000 }));

            LinkEntity linkEntity = new LinkEntity(Constants.ProjectTasks.LogicalName, Constants.TaskIdentifiers.LogicalName, Constants.ProjectTasks.TaskIdentifier, Constants.TaskIdentifiers.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(true),
                EntityAlias = "TI"
            };
            if (TaskIdentifier > 0)
                linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.IdentifierNumber, ConditionOperator.Equal, TaskIdentifier));
            if (!string.IsNullOrEmpty(WBSId))
                linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.WBSID, ConditionOperator.Equal, WBSId));
            if (projectTemplateEntityReference != null)
            {
                linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.ProjectTemplateId, ConditionOperator.Equal, projectTemplateEntityReference.Id));
            }
            queryExpression.LinkEntities.Add(linkEntity);
            queryExpression.TopCount = new int?(10);
            queryExpression.Orders.Add(new OrderExpression(Constants.ProjectTasks.ParentTask, OrderType.Ascending));
            queryExpression.Orders.Add(new OrderExpression(Constants.ProjectTasks.ContractID, OrderType.Ascending));
            return service.RetrieveMultiple(queryExpression);
        }


        public static void CalculateRollup(IOrganizationService _service, string fieldName, EntityReference targetRef)
        {

            //  Execute the Request
            CalculateRollupFieldRequest req = new CalculateRollupFieldRequest()
            {
                FieldName = fieldName,
                Target = targetRef
            };
            CalculateRollupFieldResponse resp = (CalculateRollupFieldResponse)_service.Execute(req);
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


        public static List<Entity> RetrieveAllProjectTaskByUnit(ITracingService tracer, IOrganizationService service, EntityReference unitEntityReference)
        {
            int pageNumber = 1;
            string pagingCookie = string.Empty;
            List<Entity> projectTaskEntityList = new List<Entity>();

            QueryExpression queryExpression = new QueryExpression(Constants.ProjectTasks.LogicalName)
            {
                ColumnSet = new ColumnSet(Constants.ProjectTasks.PrimaryKey)
            };
            LinkEntity projectLinkEntity = new LinkEntity(Constants.ProjectTasks.LogicalName, Constants.Projects.LogicalName, Constants.ProjectTasks.Project, Constants.Projects.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(Constants.Projects.PrimaryKey),
                EntityAlias = "PR"
            };
            projectLinkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.Status.StateCode, ConditionOperator.Equal, 0));
            projectLinkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.Projects.Unit, ConditionOperator.Equal, unitEntityReference.Id));
            queryExpression.LinkEntities.Add(projectLinkEntity);

            while (true)
            {
                queryExpression.PageInfo = new PagingInfo();
                queryExpression.PageInfo.Count = 250;
                queryExpression.PageInfo.PageNumber = pageNumber;
                if (!string.IsNullOrEmpty(pagingCookie))
                    queryExpression.PageInfo.PagingCookie = pagingCookie;

                EntityCollection entityCollection = service.RetrieveMultiple(queryExpression);
                if (entityCollection.Entities.Count > 0)
                    projectTaskEntityList.AddRange(entityCollection.Entities.ToList());

                if (entityCollection.MoreRecords)
                {
                    pageNumber++;
                    pagingCookie = entityCollection.PagingCookie;
                }
                else
                    break;

            }
            return projectTaskEntityList;

        }

        public static void PerformExecuteMultipleRequest(IOrganizationService service, OrganizationRequestCollection orgRequestCollection)
        {
            ExecuteMultipleRequest multipleReq = new ExecuteMultipleRequest()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = true
                },
                Requests = orgRequestCollection
            };

            try
            {
                ExecuteMultipleResponse multipleResponse = service.Execute(multipleReq) as ExecuteMultipleResponse;
            }
            catch (Exception ex)
            {

            }

        }

        public static EntityCollection RetrieveJobVenodrsByRenowalkID(ITracingService tracer, IOrganizationService service, string renowalkID)
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
                            AttributeName = Constants.JobVendors.StartDate,
                            Operator = ConditionOperator.NotNull
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.JobVendors.EndDate,
                            Operator = ConditionOperator.NotNull
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.JobVendors.VendorID,
                            Operator = ConditionOperator.NotNull
                        }
                    }
                },
                TopCount = 1000,
                Orders = { new OrderExpression(Constants.JobVendors.StartDate, OrderType.Ascending) }
            };

            LinkEntity vendorlinkEntity = new LinkEntity(Constants.JobVendors.LogicalName, Constants.Vendors.LogicalName, Constants.JobVendors.VendorID, Constants.Vendors.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(Constants.Vendors.PrimaryKey, Constants.Vendors.AccountCode, Constants.Vendors.Name),
                EntityAlias = "V"
            };
            vendorlinkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.Vendors.AccountCode, ConditionOperator.NotNull));
            Query.LinkEntities.Add(vendorlinkEntity);

            LinkEntity joblinkEntity = new LinkEntity(Constants.JobVendors.LogicalName, Constants.Jobs.LogicalName, Constants.JobVendors.JobID, Constants.Jobs.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(Constants.Jobs.PrimaryKey, Constants.Jobs.RenowalkID),
                EntityAlias = "J"
            };
            joblinkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.Jobs.RenowalkID, ConditionOperator.Equal, renowalkID));
            Query.LinkEntities.Add(joblinkEntity);


            return service.RetrieveMultiple(Query);
        }

        public static EntityCollection RetrieveChangeOrderItems(ITracingService tracer, IOrganizationService service, EntityReference changeOrderEntityReference)
        {
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.ChangeOrderItems.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.ChangeOrderItems.ChangeOrder,
                            Operator = ConditionOperator.Equal,
                            Values = { changeOrderEntityReference.Id }
                        }
                    }
                },
                TopCount = 100
            };

            LinkEntity linkEntity = new LinkEntity(Constants.ChangeOrderItems.LogicalName, Constants.JobCategories.LogicalName, Constants.ChangeOrderItems.JobCategory, Constants.JobCategories.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(Constants.JobCategories.GLCode),
                EntityAlias = "JC"
            };
            Query.LinkEntities.Add(linkEntity);


            return service.RetrieveMultiple(Query);
        }
        public static EntityCollection RetrieveAllBudjgetApprovers(ITracingService tracer, IOrganizationService service, int market, int approverLevel)
        {
            QueryExpression queryExpression = new QueryExpression
            {
                EntityName = Constants.BudgetApprovers.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.BudgetApprovers.ApproverID,
                            Operator = ConditionOperator.NotNull
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.Status.StateCode,
                            Operator = ConditionOperator.Equal,
                            Values = { 0 }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.BudgetApprovers.Market,
                            Operator = ConditionOperator.Equal,
                            Values = { market }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.BudgetApprovers.Level,
                            Operator = ConditionOperator.Equal,
                            Values = { approverLevel }
                        }
                    }
                },
                TopCount = 100
            };

            LinkEntity linkEntity = new LinkEntity(Constants.BudgetApprovers.LogicalName, Constants.SystemUsers.LogicalName, Constants.BudgetApprovers.ApproverID, Constants.SystemUsers.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(Constants.SystemUsers.PrimaryEmail, Constants.SystemUsers.UserName),
                EntityAlias = "U"
            };
            queryExpression.LinkEntities.Add(linkEntity);


            return service.RetrieveMultiple(queryExpression);
        }


        public static void SendRequestForApprovalEmail(ITracingService tracer, IOrganizationService service, Entity changeOrderEntity, EntityCollection fromEntitycollection, EntityCollection toEntitycollection, string ServerUrl)
        {
            string recordUrl = $"{ServerUrl}/main.aspx?etn={changeOrderEntity.LogicalName}&pagetype=entityrecord&id={changeOrderEntity.Id.ToString()}";
            tracer.Trace($"Record Url : {recordUrl}");
            Entity emailActivity = new Entity(Constants.Emails.LogicalName);
            emailActivity[Constants.Emails.Subject] = $"Change Order Submitted for Approval: {changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectID).Name}";
            emailActivity[Constants.Emails.To] = toEntitycollection;
            emailActivity[Constants.Emails.From] = fromEntitycollection;
            emailActivity[Constants.Emails.DirectionCode] = true;
            emailActivity[Constants.Emails.RegardingObject] = changeOrderEntity.ToEntityReference();
            emailActivity[Constants.Emails.Description] = $"{changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.Requestor).Name} has submitted a change order for your approval for the project {changeOrderEntity.GetAttributeValue<EntityReference>(Constants.ChangeOrders.ProjectID).Name}.<br/> Please review the change order for approval or rejection.<br/><br/> Click here to access the change order. <br/><a href ='{recordUrl}'>{changeOrderEntity.GetAttributeValue<string>(Constants.ChangeOrders.Name)}</a>   ";

            emailActivity.Id = service.Create(emailActivity);
        }

        public static EntityCollection ApproverOrderList(ITracingService tracer, IOrganizationService service, int level, int market, Guid currentSystemUserID)
        {
            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.BudgetApprovers.LogicalName,
                ColumnSet = new ColumnSet(true),
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.BudgetApprovers.Level,
                            Operator = ConditionOperator.Equal,
                            Values = { level }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.BudgetApprovers.Market,
                            Operator = ConditionOperator.Equal,
                            Values = { market }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.BudgetApprovers.ApproverID,
                            Operator = ConditionOperator.Equal,
                            Values = { currentSystemUserID }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.Status.StateCode,
                            Operator = ConditionOperator.Equal,
                            Values = { 0 }
                        }
                    }
                },
                TopCount = 100,
            };
            return service.RetrieveMultiple(Query);
        }

    }

}

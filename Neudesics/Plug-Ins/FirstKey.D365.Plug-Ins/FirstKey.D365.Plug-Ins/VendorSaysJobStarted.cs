using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace FirstKey.D365.Plug_Ins
{
    public class VendorSaysJobStarted : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        private const string TURNPROCESS_PROJECT_TEMPLATE = "TURNPROCESS_PROJECT_TEMPLATE";
        private const string INITIALRENOVATION_PROJECT_TEMPLATE = "INITIALRENOVATION_PROJECT_TEMPLATE";

        public VendorSaysJobStarted(string unsecureConfig, string secureConfig)
        {
            _secureConfig = secureConfig;
            _unsecureConfig = unsecureConfig;
        }
        #endregion
        public void Execute(IServiceProvider serviceProvider)
        {
            ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory factory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = factory.CreateOrganizationService(context.UserId);

            ProjectTemplateSettings projectTemplateSettings = null;
            if (!string.IsNullOrEmpty(_unsecureConfig))
            {
                StringReader stringReader = new StringReader(_unsecureConfig);
                XmlSerializer serializer = new XmlSerializer(typeof(ProjectTemplateSettings));

                projectTemplateSettings = (ProjectTemplateSettings)serializer.Deserialize(stringReader);
            }

            if (projectTemplateSettings == null)
                throw new InvalidPluginExecutionException(OperationStatus.Failed, "projectTemplateSettings is NULL.UnSecure Plugin Configuration Not Found.");

            EntityReference projectTaskEntityReference = context.InputParameters.Contains(Constants.TARGET) ? context.InputParameters[Constants.TARGET] as EntityReference : null;
            if (projectTaskEntityReference == null) return;

            try
            {
                ExecuteContext(tracer, service, projectTaskEntityReference, projectTemplateSettings);
                context.OutputParameters[Constants.CustomActionParam.IsSuccess] = true;
                context.OutputParameters[Constants.CustomActionParam.ErrorMessage] = string.Empty;
            }
            catch (Exception ex)
            {
                tracer.Trace(ex.Message + ex.StackTrace);
                context.OutputParameters[Constants.CustomActionParam.IsSuccess] = false;
                context.OutputParameters[Constants.CustomActionParam.ErrorMessage] = ex.Message;
            }
        }

        public static void ExecuteContext(ITracingService tracer, IOrganizationService service, EntityReference projectTaskEntityReference, ProjectTemplateSettings projectTemplateSettings)
        {
            Entity projectTaskEntity = service.Retrieve(projectTaskEntityReference.LogicalName, projectTaskEntityReference.Id, new ColumnSet(true));
            if (projectTaskEntity is Entity && projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.Project))
            {
                Entity projectEntity = service.Retrieve(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.Project).LogicalName, projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.Project).Id, new ColumnSet(true));
                if (projectEntity is Entity && projectEntity.Attributes.Contains(Constants.Projects.ProjectTemplate) && projectEntity.Attributes.Contains(Constants.Projects.Unit) &&
                    projectEntity.Attributes.Contains(Constants.Projects.RenowalkID))
                {
                    Mapping mapping = (
        from m in projectTemplateSettings.Mappings
        where m.Key.Equals(projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate).Id.ToString(), StringComparison.OrdinalIgnoreCase)
        select m).FirstOrDefault<Mapping>();

                    if (mapping is Mapping)
                    {


                        tracer.Trace("Project Entity Object Received with Unit value.");
                        Entity propertyEntity = service.Retrieve(projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Unit).LogicalName, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Unit).Id, new ColumnSet(true));
                        if (propertyEntity is Entity && (propertyEntity.Attributes.Contains(Constants.Units.UnitId) || propertyEntity.Attributes.Contains(Constants.Units.SFCode)))
                        {

                            Events gridEvent;
                            if (projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.ParentTask) && projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.ContractID))
                            {

                                EntityCollection entityCollection = RetrieveChildProjectTaskFromParentProjectTask(tracer, service, projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.ParentTask), projectTaskEntity.ToEntityReference());
                                if (entityCollection.Entities.Count == 0)
                                {
                                    if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                                        gridEvent = Events.VENDORS_SAYS_JOB_STARTED;
                                    else
                                        gridEvent = Events.IR_VENDORS_SAYS_JOB_STARTED;
                                    //Publish VENDORS_SAYS_JOB_STARTED message to Grid...
                                    CreateOutGoingAzureIntegrationCallRecord(service, tracer, (propertyEntity.Attributes.Contains(Constants.Units.UnitId)) ? propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId) : propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode),
                                        projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID), projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.ContractID), gridEvent,
                                        projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), projectTemplateSettings);

                                    Entity tmpPrj = new Entity(projectEntity.LogicalName);
                                    tmpPrj.Id = projectEntity.Id;
                                    tmpPrj[Constants.Projects.ActualJobStartDate] = DateTime.Now;

                                    service.Update(tmpPrj);
                                }
                                else
                                {
                                    int notStartedCount = entityCollection.Entities.Where(e => e.Attributes.Contains(Constants.Status.StatusCode) && e.GetAttributeValue<OptionSetValue>(Constants.Status.StatusCode).Value == 1).Count();
                                    tracer.Trace($"Not Started Count : {notStartedCount}.");
                                    tracer.Trace($"Total Child Task Count : {entityCollection.Entities.Count}.");
                                    if (notStartedCount == entityCollection.Entities.Count)
                                    {
                                        //Publish  VENDORS_SAYS_JOB_STARTED message to Grid
                                        if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                                            gridEvent = Events.VENDORS_SAYS_JOB_STARTED;
                                        else
                                            gridEvent = Events.IR_VENDORS_SAYS_JOB_STARTED;
                                        //Publish message to Grid...
                                        CreateOutGoingAzureIntegrationCallRecord(service, tracer, (propertyEntity.Attributes.Contains(Constants.Units.UnitId)) ? propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId) : propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode),
                                        projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID), projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.ContractID), gridEvent,
                                        projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), projectTemplateSettings);

                                        Entity tmpPrj = new Entity(projectEntity.LogicalName);
                                        tmpPrj.Id = projectEntity.Id;
                                        tmpPrj[Constants.Projects.ActualJobStartDate] = DateTime.Now;

                                        service.Update(tmpPrj);
                                    }
                                    if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                                        gridEvent = Events.VENDOR_SAYS_CONTRACT_STARTED;
                                    else
                                        gridEvent = Events.IR_VENDOR_SAYS_CONTRACT_STARTED;

                                    CreateOutGoingAzureIntegrationCallRecord(service, tracer, (propertyEntity.Attributes.Contains(Constants.Units.UnitId)) ? propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId) : propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode),
            projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID), projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.ContractID), gridEvent,
            projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), projectTemplateSettings);

                                }

                            }
                            else if (!projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.ParentTask) && !projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.ContractID))
                            {
                                tracer.Trace("Only updating Actual Job Start Date on Project as Task is not Child.");

                                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                                    gridEvent = Events.VENDORS_SAYS_JOB_STARTED;
                                else
                                    gridEvent = Events.IR_VENDORS_SAYS_JOB_STARTED;
                                //Publish VENDORS_SAYS_JOB_STARTED message to Grid...
                                tracer.Trace($"Publishing OutGoing message : {gridEvent.ToString()}.");
                                CreateOutGoingAzureIntegrationCallRecord(service, tracer, (propertyEntity.Attributes.Contains(Constants.Units.UnitId)) ? propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId) : propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode),
                                    projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID), projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.ContractID), gridEvent,
                                    projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), projectTemplateSettings);

                                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                                    gridEvent = Events.VENDOR_SAYS_CONTRACT_STARTED;
                                else
                                    gridEvent = Events.IR_VENDOR_SAYS_CONTRACT_STARTED;

                                //Publish CONTRACT_STARTED message to Grid...
                                tracer.Trace($"Publishing OutGoing message : {gridEvent.ToString()}.");
                                CreateOutGoingAzureIntegrationCallRecord(service, tracer, (propertyEntity.Attributes.Contains(Constants.Units.UnitId)) ? propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId) : propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode),
        projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID), projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.ContractID), gridEvent,
        projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), projectTemplateSettings);

                                Entity tmpPrj = new Entity(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.Project).LogicalName);
                                tmpPrj.Id = projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.Project).Id;
                                tmpPrj[Constants.Projects.ActualJobStartDate] = DateTime.Now;

                                service.Update(tmpPrj);
                            }
                        }
                    }
                }

            }
        }

        private static EntityCollection RetrieveChildProjectTaskFromParentProjectTask(ITracingService tracer, IOrganizationService service, EntityReference parentProjectTaskEntityReference, EntityReference childProjectTaskEntityReference)
        {
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
                            AttributeName = Constants.ProjectTasks.ParentTask,
                            Operator = ConditionOperator.Equal,
                            Values = { parentProjectTaskEntityReference.Id }
                        },
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTasks.PrimaryKey,
                            Operator = ConditionOperator.NotEqual,
                            Values = { childProjectTaskEntityReference.Id }
                        }
                    }
                },
                TopCount = 100
            };

            return service.RetrieveMultiple(Query);
        }

        private static void CreateOutGoingAzureIntegrationCallRecord(IOrganizationService _service, ITracingService tracer, string propertyID, string renowalkID, string Contract_Code, Events gridEvent, EntityReference projectTemplateEntityReference, ProjectTemplateSettings projectTemplateSettings)
        {
            Mapping mapping = (
                    from m in projectTemplateSettings.Mappings
                    where m.Key.Equals(projectTemplateEntityReference.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                    select m).FirstOrDefault<Mapping>();

            if (mapping is Mapping)
            {
                tracer.Trace($"Project Template is : {mapping.Name}");

                List<GridEvent<DataPayLoad>> gridEventDataPayloadList = new List<GridEvent<DataPayLoad>>();
                GridEvent<DataPayLoad> gridEventDataPayload = new GridEvent<DataPayLoad>();
                gridEventDataPayload.EventTime = DateTime.Now.ToString();
                gridEventDataPayload.EventType = "allEvents";
                gridEventDataPayload.Id = Guid.NewGuid().ToString();
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    gridEventDataPayload.Subject = $"Turn Process : {gridEvent.ToString()}";
                else
                    gridEventDataPayload.Subject = $"Initial Renovation : {gridEvent.ToString()}";
                gridEventDataPayload.data = new DataPayLoad();
                gridEventDataPayload.data.Date1 = DateTime.Now.ToString();
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    gridEventDataPayload.data.Event = gridEvent;
                else
                    gridEventDataPayload.data.Event = gridEvent;
                gridEventDataPayload.data.IsForce = false;
                gridEventDataPayload.data.PropertyID = propertyID;
                gridEventDataPayload.data.RenowalkID = renowalkID;
                gridEventDataPayload.data.Contract_Code = Contract_Code;

                gridEventDataPayloadList.Add(gridEventDataPayload);

                Entity azIntCallEntity = new Entity(Constants.AzureIntegrationCalls.LogicalName);
                azIntCallEntity[Constants.AzureIntegrationCalls.EventData] = CommonMethods.Serialize(gridEventDataPayloadList);
                azIntCallEntity[Constants.AzureIntegrationCalls.Direction] = true;
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = gridEvent.ToString();
                else
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = gridEvent.ToString();
                _service.Create(azIntCallEntity);
            }
            else
            {
                tracer.Trace($"Project Template Mapping Not found in PlugIn Setting for Project Template : {projectTemplateEntityReference.Id.ToString()}");
            }
        }
    }
}
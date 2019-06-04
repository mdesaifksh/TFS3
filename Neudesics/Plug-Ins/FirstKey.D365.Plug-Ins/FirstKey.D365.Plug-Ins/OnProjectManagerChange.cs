using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace FirstKey.D365.Plug_Ins
{
    public class OnProjectManagerChange : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        private const string TURNPROCESS_PROJECT_TEMPLATE = "TURNPROCESS_PROJECT_TEMPLATE";
        private const string INITIALRENOVATION_PROJECT_TEMPLATE = "INITIALRENOVATION_PROJECT_TEMPLATE";


        public OnProjectManagerChange(string unsecureConfig, string secureConfig)
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
            IOrganizationService systemuser_service = factory.CreateOrganizationService(null);

            ProjectTemplateSettings projectTemplateSettings = null;

            if (!string.IsNullOrEmpty(_unsecureConfig))
            {
                StringReader stringReader = new StringReader(_unsecureConfig);
                XmlSerializer serializer = new XmlSerializer(typeof(ProjectTemplateSettings));

                projectTemplateSettings = (ProjectTemplateSettings)serializer.Deserialize(stringReader);
            }

            if (projectTemplateSettings == null)
                throw new InvalidPluginExecutionException(OperationStatus.Failed, "projectTemplateSettings is NULL.UnSecure Plugin Configuration Not Found.");

            Entity projectEntity = null;
            if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.Projects.LogicalName)
                return;

            try
            {
                //Entity entity = (Entity)context.InputParameters["Target"];

                //TODO: Do stuff
                switch (context.MessageName)
                {
                    case Constants.Messages.Update:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity && context.PostEntityImages.Contains(Constants.POST_IMAGE))
                            projectEntity = context.PostEntityImages[Constants.POST_IMAGE] as Entity;
                        else
                            return;
                        break;
                }

                if (projectEntity == null || context.Depth > 2)
                {
                    tracer.Trace($"Project entity is Null OR Context Depth is higher than 1. Actual Depth is : {context.Depth}");
                    return;
                }

                if (projectEntity is Entity && projectEntity.Attributes.Contains(Constants.Projects.Unit) && projectEntity.Attributes.Contains(Constants.Projects.ProjectTemplate))
                {
                    tracer.Trace("Project Entity Object Received with Unit value.");
                    Entity propertyEntity = service.Retrieve(projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Unit).LogicalName, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.Unit).Id, new ColumnSet(true));
                    if (propertyEntity is Entity && (propertyEntity.Attributes.Contains(Constants.Units.UnitId) || propertyEntity.Attributes.Contains(Constants.Units.SFCode)))
                    {
                        tracer.Trace($"Unit found with Unit ID {propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId)}");
                        tracer.Trace("Creating Incoming Integration Record.");
                        CreateInComingGoingAzureIntegrationCallRecord(systemuser_service, tracer, (propertyEntity.Attributes.Contains(Constants.Units.UnitId)) ? propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId) : propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode), projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), projectTemplateSettings);
                        tracer.Trace("Incoming Integration Record successfully created.");
                        if (projectEntity.Attributes.Contains(Constants.Projects.ProjectManager) && projectEntity.Attributes.Contains(Constants.Projects.RenowalkID))
                            CreateOutGoingAzureIntegrationCallRecord(systemuser_service, tracer, projectEntity, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), projectTemplateSettings, (propertyEntity.Attributes.Contains(Constants.Units.UnitId)) ? propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId) : propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode));
                        else
                            tracer.Trace("Project Record does NOT have Project Manager or Renowalk ID.");

                        tracer.Trace("Assigning Project Manager as Owner for each Project Task.");
                        AssignProjectManagerAsTaskOwner(tracer, service, projectEntity);

                    }
                    else
                        tracer.Trace("Property Entity Record Not Found.");
                }
                else
                    tracer.Trace("Project entity Object not found OR Project Entity does not have Unit or does not have Project Template.");
            }
            catch (Exception ex)
            {
                tracer.Trace(ex.Message + ex.StackTrace);
                //UpdateAzureIntegrationCallErrorDetails(service, azureIntegrationCallEntity.ToEntityReference(), ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private static void AssignProjectManagerAsTaskOwner(ITracingService tracer, IOrganizationService service, Entity projectEntity)
        {
            tracer.Trace($"Retrieving All Task for Project.");
            if (projectEntity.Attributes.Contains(Constants.Projects.ProjectManager))
            {
                EntityCollection projectTaskEntityCollection = RetrieveAllProjectTaskByProject(tracer, service, projectEntity.ToEntityReference());
                foreach (Entity projectTaskEntity in projectTaskEntityCollection.Entities)
                {
                    Entity tmpPT = new Entity(projectTaskEntity.LogicalName);
                    tmpPT.Id = projectTaskEntity.Id;
                    tmpPT[Constants.ProjectTasks.Owner] = projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectManager);

                    service.Update(tmpPT);
                }
            }
        }

        public static EntityCollection RetrieveAllProjectTaskByProject(ITracingService tracer, IOrganizationService service, EntityReference projectEntityRefernce)
        {
            LinkEntity linkEntity = new LinkEntity(Constants.ProjectTasks.LogicalName, Constants.TaskIdentifiers.LogicalName, Constants.ProjectTasks.TaskIdentifier, Constants.TaskIdentifiers.PrimaryKey, JoinOperator.Inner)
            {
                Columns = new ColumnSet(true),
                EntityAlias = "TI"
            };
            linkEntity.LinkCriteria.AddCondition(new ConditionExpression(Constants.TaskIdentifiers.IdentifierNumber, ConditionOperator.NotIn, new int[] { 18, 19, 20, 21, 217, 218, 219, 220 }));

            QueryExpression Query = new QueryExpression
            {
                EntityName = Constants.ProjectTasks.LogicalName,
                ColumnSet = new ColumnSet(true),
                LinkEntities = { linkEntity },
                Criteria = new FilterExpression
                {
                    FilterOperator = LogicalOperator.And,
                    Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = Constants.ProjectTasks.Project,
                            Operator = ConditionOperator.Equal,
                            Values = { projectEntityRefernce.Id }
                        }
                    }
                },
                TopCount = 100
            };

            return service.RetrieveMultiple(Query);
        }

        private void CreateOutGoingAzureIntegrationCallRecord(IOrganizationService _service, ITracingService tracer, Entity projectEntity, EntityReference projectTemplateEntityReference, ProjectTemplateSettings projectTemplateSettings, string propertyID)
        {
            Mapping mapping = (
                    from m in projectTemplateSettings.Mappings
                    where m.Key.Equals(projectTemplateEntityReference.Id.ToString(), StringComparison.OrdinalIgnoreCase)
                    select m).FirstOrDefault<Mapping>();

            if (mapping is Mapping)
            {
                tracer.Trace($"Project Template is : {mapping.Name}");
                Entity systemuserEntity = _service.Retrieve(projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectManager).LogicalName, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectManager).Id, new ColumnSet(true));
                if (systemuserEntity is Entity && systemuserEntity.Attributes.Contains(Constants.SystemUsers.UserName))
                {

                    List<GridEvent<DataPayLoad>> gridEventDataPayloadList = new List<GridEvent<DataPayLoad>>();
                    GridEvent<DataPayLoad> gridEventDataPayload = new GridEvent<DataPayLoad>();
                    gridEventDataPayload.EventTime = DateTime.Now.ToString();
                    gridEventDataPayload.EventType = "allEvents";
                    gridEventDataPayload.Id = Guid.NewGuid().ToString();
                    if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                        gridEventDataPayload.Subject = $"Turn Process : {Events.ASSIGN_PROJECT_MANAGER.ToString()}";
                    else
                        gridEventDataPayload.Subject = $"Initial Renovation : {Events.IR_ASSIGN_PROJECT_MANAGER.ToString()}";
                    gridEventDataPayload.data = new DataPayLoad();
                    gridEventDataPayload.data.Date1 = DateTime.Now.ToString();
                    if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                        gridEventDataPayload.data.Event = Events.ASSIGN_PROJECT_MANAGER;
                    else
                        gridEventDataPayload.data.Event = Events.IR_ASSIGN_PROJECT_MANAGER;
                    gridEventDataPayload.data.IsForce = false;
                    gridEventDataPayload.data.PropertyID = propertyID;
                    gridEventDataPayload.data.EmailID = systemuserEntity.GetAttributeValue<string>(Constants.SystemUsers.UserName);
                    gridEventDataPayload.data.FotoNotesID = "";
                    gridEventDataPayload.data.JobID = "";
                    gridEventDataPayload.data.RenowalkID = projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID);

                    gridEventDataPayloadList.Add(gridEventDataPayload);

                    Entity azIntCallEntity = new Entity(Constants.AzureIntegrationCalls.LogicalName);
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventData] = CommonMethods.Serialize(gridEventDataPayloadList);
                    azIntCallEntity[Constants.AzureIntegrationCalls.Direction] = true;
                    if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                        azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.ASSIGN_PROJECT_MANAGER.ToString();
                    else
                        azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.IR_ASSIGN_PROJECT_MANAGER.ToString();
                    _service.Create(azIntCallEntity);
                }
                else
                {
                    tracer.Trace($"System User (Project Manager) Not Found or Primary Email (internalemailaddress) Not found for System User (Project Manager).");
                }
            }
            else
            {
                tracer.Trace($"Project Template Mapping Not found in PlugIn Setting for Project Template : {projectTemplateEntityReference.Id.ToString()}");
            }
        }

        private void CreateInComingGoingAzureIntegrationCallRecord(IOrganizationService _service, ITracingService tracer, string propertyID, EntityReference projectTemplateEntityReference, ProjectTemplateSettings projectTemplateSettings)
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
                    gridEventDataPayload.Subject = $"Turn Process : {Events.ASSIGN_PROJECT_MANAGER.ToString()}";
                else
                    gridEventDataPayload.Subject = $"Initial Renovation : {Events.IR_ASSIGN_PROJECT_MANAGER.ToString()}";
                gridEventDataPayload.data = new DataPayLoad();
                gridEventDataPayload.data.Date1 = DateTime.Now.ToString();
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    gridEventDataPayload.data.Event = Events.ASSIGN_PROJECT_MANAGER;
                else
                    gridEventDataPayload.data.Event = Events.IR_ASSIGN_PROJECT_MANAGER;
                gridEventDataPayload.data.IsForce = false;
                gridEventDataPayload.data.PropertyID = propertyID;

                gridEventDataPayloadList.Add(gridEventDataPayload);

                Entity azIntCallEntity = new Entity(Constants.AzureIntegrationCalls.LogicalName);
                azIntCallEntity[Constants.AzureIntegrationCalls.EventData] = CommonMethods.Serialize(gridEventDataPayloadList);
                azIntCallEntity[Constants.AzureIntegrationCalls.Direction] = false;
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.ASSIGN_PROJECT_MANAGER.ToString();
                else
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.IR_ASSIGN_PROJECT_MANAGER.ToString();
                _service.Create(azIntCallEntity);
            }
            else
            {
                tracer.Trace($"Project Template Mapping Not found in PlugIn Setting for Project Template : {projectTemplateEntityReference.Id.ToString()}");
            }
        }
    }
}

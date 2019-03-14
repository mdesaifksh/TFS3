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
    public class OnAppointmentCreate : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        private const string TURNPROCESS_PROJECT_TEMPLATE = "TURNPROCESS_PROJECT_TEMPLATE";
        private const string INITIALRENOVATION_PROJECT_TEMPLATE = "INITIALRENOVATION_PROJECT_TEMPLATE";


        public OnAppointmentCreate(string unsecureConfig, string secureConfig)
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


            Entity appointmentEntity = null;
            if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.Appointments.LogicalName)
                return;

            try
            {
                //Entity entity = (Entity)context.InputParameters["Target"];

                //TODO: Do stuff
                switch (context.MessageName)
                {
                    case Constants.Messages.Create:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity)
                            appointmentEntity = context.InputParameters[Constants.TARGET] as Entity;
                        else
                            return;
                        break;
                    case Constants.Messages.Update:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity && context.PostEntityImages.Contains(Constants.POST_IMAGE))
                            appointmentEntity = context.PostEntityImages[Constants.POST_IMAGE] as Entity;
                        else
                            return;
                        break;
                }

                if (appointmentEntity == null || context.Depth > 1)
                {
                    tracer.Trace($"Appointment entity is Null OR Context Depth is higher than 1. Actual Depth is : {context.Depth}");
                    return;
                }
                if (!appointmentEntity.Attributes.Contains(Constants.Appointments.Regarding) || !appointmentEntity.Attributes.Contains(Constants.Appointments.ScheduledStart))
                {
                    tracer.Trace($"Appointment missing regardingobject field or Scheduled Start.");
                    return;
                }

                if (!appointmentEntity.GetAttributeValue<EntityReference>(Constants.Appointments.Regarding).LogicalName.Equals(Constants.ProjectTasks.LogicalName))
                {
                    tracer.Trace($"Appointment Regarding Object is NOT type of Project Task.");
                    return;
                }

                if (appointmentEntity is Entity)
                {
                    tracer.Trace("Retrieving Project Task Record.");
                    Entity projectTaskEntity = service.Retrieve(appointmentEntity.GetAttributeValue<EntityReference>(Constants.Appointments.Regarding).LogicalName, appointmentEntity.GetAttributeValue<EntityReference>(Constants.Appointments.Regarding).Id, new ColumnSet(true));
                    if (projectTaskEntity is Entity && projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.TaskIdentifier))
                    {
                        tracer.Trace("Project Task Entity Object Received with Unit value.");
                        Entity taskIdentifierEntity = service.Retrieve(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).LogicalName, projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.TaskIdentifier).Id, new ColumnSet(true));
                        if (taskIdentifierEntity is Entity)
                        {
                            tracer.Trace("Task Identifier Entity Object Received with Unit value.");
                            if (taskIdentifierEntity.Attributes.Contains(Constants.TaskIdentifiers.WBSID) && projectTaskEntity.Attributes.Contains(Constants.ProjectTasks.WBSID) && taskIdentifierEntity.Attributes.Contains(Constants.TaskIdentifiers.ProjectTemplateId))
                            {
                                tracer.Trace($"Task Identifier & Project Task Contains WBS ID. . Task Identifier WBS ID : {taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID)}, Project Task WBS ID : {projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID)}");
                                if ((taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID).Equals("5", StringComparison.OrdinalIgnoreCase) && projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID).Equals("5", StringComparison.OrdinalIgnoreCase))
                                    || (taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID).Equals("3", StringComparison.OrdinalIgnoreCase) && projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID).Equals("3", StringComparison.OrdinalIgnoreCase)))
                                {
                                    Entity propertyEntity = service.Retrieve(projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.UnitId).LogicalName, projectTaskEntity.GetAttributeValue<EntityReference>(Constants.ProjectTasks.UnitId).Id, new ColumnSet(true));
                                    if (propertyEntity is Entity && (propertyEntity.Attributes.Contains(Constants.Units.UnitId) || propertyEntity.Attributes.Contains(Constants.Units.SFCode)))
                                    {
                                        if (propertyEntity.Attributes.Contains(Constants.Units.UnitId))
                                            tracer.Trace($"Unit found with Unit ID {propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId)}");
                                        else if (propertyEntity.Attributes.Contains(Constants.Units.SFCode))
                                            tracer.Trace($"Unit found with SF Code ID {propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode)}");
                                        tracer.Trace("Creating Incoming Integration Record.");
                                            CreateInComingGoingAzureIntegrationCallRecord(service, tracer, (propertyEntity.Attributes.Contains(Constants.Units.UnitId)) ? propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId) : propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode),
                                                appointmentEntity.GetAttributeValue<DateTime>(Constants.Appointments.CreatedOn), appointmentEntity.GetAttributeValue<DateTime>(Constants.Appointments.ScheduledStart),
                                                appointmentEntity.GetAttributeValue<DateTime>(Constants.Appointments.ScheduledEnd),taskIdentifierEntity.GetAttributeValue<EntityReference>(Constants.TaskIdentifiers.ProjectTemplateId), projectTemplateSettings);
                                        tracer.Trace("Incoming Integration Record successfully created.");
                                    }
                                    else
                                        tracer.Trace("Property Entity Record Not Found.");
                                }
                                else if (taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID).Equals("3", StringComparison.OrdinalIgnoreCase) && projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID).Equals("3", StringComparison.OrdinalIgnoreCase))
                                {

                                }
                                else
                                    tracer.Trace($"Project Task Record is Not correct. Task Identifier WBS ID : {taskIdentifierEntity.GetAttributeValue<string>(Constants.TaskIdentifiers.WBSID)}, Project Task WBS ID : {projectTaskEntity.GetAttributeValue<string>(Constants.ProjectTasks.WBSID)}");
                            }
                            else
                                tracer.Trace("Either Task Identifier or Project Task Record does Not contain WBS ID.");
                        }
                        else
                            tracer.Trace("Task Identifier Record Not Found.");
                    }
                    else
                        tracer.Trace($"Project Task Record Not Found Or Not contains Task Identifier. Task Status : {projectTaskEntity.GetAttributeValue<OptionSetValue>(Constants.Status.StatusCode).Value}");
                }
            }
            catch (Exception e)
            {
                throw new InvalidPluginExecutionException(e.Message);
            }
        }


        private void CreateInComingGoingAzureIntegrationCallRecord(IOrganizationService _service, ITracingService tracer, string propertyID, DateTime appointmentCreatedOn, DateTime appointmentSchStart, DateTime appointmentSchEnd, EntityReference projectTemplateEntityReference, ProjectTemplateSettings projectTemplateSettings)
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
                    gridEventDataPayload.Subject = $"Turn Process : {Events.MARKET_SCHEDULES_PRE_MOVE_OUT.ToString()}";
                else
                    gridEventDataPayload.Subject = $"Initial Renovation : {Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION.ToString()}";

                gridEventDataPayload.data = new DataPayLoad();
                gridEventDataPayload.data.Date1 = appointmentCreatedOn.ToString();
                tracer.Trace($"App Scheduled Start : {appointmentSchStart.ToString()}");
                tracer.Trace($"App Scheduled End : {appointmentSchEnd.ToString()}");
                gridEventDataPayload.data.Date2 = appointmentSchStart.ToString();
                gridEventDataPayload.data.Date3 = appointmentSchEnd.ToString();
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    gridEventDataPayload.data.Event = Events.MARKET_SCHEDULES_PRE_MOVE_OUT;
                else
                    gridEventDataPayload.data.Event = Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION;
                gridEventDataPayload.data.IsForce = false;
                gridEventDataPayload.data.PropertyID = propertyID;

                gridEventDataPayloadList.Add(gridEventDataPayload);

                Entity azIntCallEntity = new Entity(Constants.AzureIntegrationCalls.LogicalName);
                azIntCallEntity[Constants.AzureIntegrationCalls.EventData] = CommonMethods.Serialize(gridEventDataPayloadList);
                azIntCallEntity[Constants.AzureIntegrationCalls.Direction] = false;
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.MARKET_SCHEDULES_PRE_MOVE_OUT.ToString();
                else
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.SCHEDULE_DUE_DILLIGENCE_INSPECTION.ToString();

                _service.Create(azIntCallEntity);
            }
            else
            {
                tracer.Trace($"Project Template Mapping Not found in PlugIn Setting for Project Template : {projectTemplateEntityReference.Id.ToString()}");
            }
        }
    }
}

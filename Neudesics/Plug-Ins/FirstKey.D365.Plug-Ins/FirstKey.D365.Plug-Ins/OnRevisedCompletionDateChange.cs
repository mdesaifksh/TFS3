﻿using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace FirstKey.D365.Plug_Ins
{
    public class OnRevisedCompletionDateChange : IPlugin
    {
        #region Secure/Unsecure Configuration Setup
        private string _secureConfig = null;
        private string _unsecureConfig = null;
        private const string TURNPROCESS_PROJECT_TEMPLATE = "TURNPROCESS_PROJECT_TEMPLATE";
        private const string INITIALRENOVATION_PROJECT_TEMPLATE = "INITIALRENOVATION_PROJECT_TEMPLATE";

        public OnRevisedCompletionDateChange(string unsecureConfig, string secureConfig)
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

            Entity projectEntity = null;
            if (!context.InputParameters.Contains(Constants.TARGET)) { return; }
            if (((Entity)context.InputParameters[Constants.TARGET]).LogicalName != Constants.Projects.LogicalName)
                return;
            try
            {
                switch (context.MessageName)
                {
                    case Constants.Messages.Update:
                        if (context.InputParameters.Contains(Constants.TARGET) && context.InputParameters[Constants.TARGET] is Entity && context.PostEntityImages.Contains(Constants.POST_IMAGE))
                            projectEntity = context.PostEntityImages[Constants.POST_IMAGE] as Entity;
                        else
                            return;
                        break;
                }

                if (projectEntity == null || context.Depth > 1)
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
                        if (projectEntity.Attributes.Contains(Constants.Projects.RevisedCompletionDate) && projectEntity.Attributes.Contains(Constants.Projects.RenowalkID))
                            CreateOutGoingAzureIntegrationCallRecord(service, tracer, projectEntity, projectEntity.GetAttributeValue<EntityReference>(Constants.Projects.ProjectTemplate), projectTemplateSettings, (propertyEntity.Attributes.Contains(Constants.Units.UnitId)) ? propertyEntity.GetAttributeValue<string>(Constants.Units.UnitId) : propertyEntity.GetAttributeValue<string>(Constants.Units.SFCode));
                        else
                            tracer.Trace("Project Record does NOT have Revised Completion Date or Renowalk ID.");

                    }
                    else
                        tracer.Trace("Property Entity Record Not Found.");
                }
                else
                    tracer.Trace("Project entity Object not found OR Project Entity does not have Unit or does not have Project Template.");

                //TODO: Do stuff
            }
            catch (Exception e)
            {
                tracer.Trace(e.Message + e.StackTrace);
            }
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

                List<GridEvent<DataPayLoad>> gridEventDataPayloadList = new List<GridEvent<DataPayLoad>>();
                GridEvent<DataPayLoad> gridEventDataPayload = new GridEvent<DataPayLoad>();
                gridEventDataPayload.EventTime = DateTime.Now.ToString();
                gridEventDataPayload.EventType = "allEvents";
                gridEventDataPayload.Id = Guid.NewGuid().ToString();
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    gridEventDataPayload.Subject = $"Turn Process : {Events.REVISED_COMPLETION_DATE.ToString()}";
                else
                    gridEventDataPayload.Subject = $"Initial Renovation : {Events.IR_REVISED_COMPLETION_DATE.ToString()}";
                gridEventDataPayload.data = new DataPayLoad();
                gridEventDataPayload.data.Date1 = projectEntity.GetAttributeValue<DateTime>(Constants.Projects.RevisedCompletionDate).ToString();
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    gridEventDataPayload.data.Event = Events.REVISED_COMPLETION_DATE;
                else
                    gridEventDataPayload.data.Event = Events.IR_REVISED_COMPLETION_DATE;
                gridEventDataPayload.data.IsForce = false;
                gridEventDataPayload.data.PropertyID = propertyID;
                gridEventDataPayload.data.EmailID = string.Empty;
                gridEventDataPayload.data.FotoNotesID = string.Empty;
                gridEventDataPayload.data.JobID = projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID);
                gridEventDataPayload.data.RenowalkID = projectEntity.GetAttributeValue<string>(Constants.Projects.RenowalkID);

                gridEventDataPayloadList.Add(gridEventDataPayload);

                Entity azIntCallEntity = new Entity(Constants.AzureIntegrationCalls.LogicalName);
                azIntCallEntity[Constants.AzureIntegrationCalls.EventData] = CommonMethods.Serialize(gridEventDataPayloadList);
                azIntCallEntity[Constants.AzureIntegrationCalls.Direction] = true;
                if (mapping.Name.Equals(TURNPROCESS_PROJECT_TEMPLATE))
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.REVISED_COMPLETION_DATE.ToString();
                else
                    azIntCallEntity[Constants.AzureIntegrationCalls.EventName] = Events.IR_REVISED_COMPLETION_DATE.ToString();
                _service.Create(azIntCallEntity);

            }
            else
            {
                tracer.Trace($"Project Template Mapping Not found in PlugIn Setting for Project Template : {projectTemplateEntityReference.Id.ToString()}");
            }
        }
    }
}